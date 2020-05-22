using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Java.Security;

namespace nfctest
{
    public class MifareNFC
    {
        #region Public Members
        public NfcAdapter NfcAdapter { get; private set; }
        public List<string> Actions { get; private set; }
        public byte[] AuthKey { get; set; } = new byte[6];
        public MifareClassic Mifare { get; private set; }
        public bool AutoHandleWriting { get; set; } = true;
        public bool AutoHandleReading { get; set; } = true;

        public Action<TagInfo?> OnNewTagDiscovered;
        public Action<int, byte[]> OnReadingBlock;
        public Action<int, byte[]> OnWritingBlock;
        #endregion
        #region Private Members
        private Activity _act;
        private bool Init = false;
        private bool _waitingForWrite = false;
        private int _writingBolck = -1;
        private byte[] _writingData = null;

        private int _readingBolck = -1;
        #endregion
        #region Public Methods
        public MifareMessage Initialize(Activity act, string[] actions, bool autoHandleWrite)
        {
            if (act == null) return MifareMessage.MIFARE_NFC_NULL_CONTEXT;
            _act = act;
            AutoHandleWriting = autoHandleWrite;
            NfcAdapter = NfcAdapter.GetDefaultAdapter(act);
            Init = true;
            if (actions == null || actions.Count() == 0)
            {
                Actions = new List<string> { NfcAdapter.ActionTagDiscovered, NfcAdapter.ActionNdefDiscovered, NfcAdapter.ActionTechDiscovered };
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(CheckIntentActions(actions))) return MifareMessage.MIFARE_NFC_INVALID_INTENT_ACTION;
            }
            if (NfcAdapter == null) return MifareMessage.MIFARE_NFC_NOT_AVAILABLE;
            if (!NfcAdapter.IsEnabled) return MifareMessage.MIFARE_NFC_DISABLED;
            return MifareMessage.MIFARE_NFC_NO_ERROR;
        }
        public MifareMessage Initialize(Activity act, bool autoHandleWrite)
        {
            return Initialize(act, null, autoHandleWrite);
        }
        public MifareMessage Initialize(Activity act)
        {
            return Initialize(act, null, true);
        }
        public MifareMessage OnResume()
        {
            if (!Init) return MifareMessage.MIFARE_NFC_NOT_INITIALIZED;
            else if (NfcAdapter == null) return MifareMessage.MIFARE_NFC_NOT_AVAILABLE;
            else if (!NfcAdapter.IsEnabled) return MifareMessage.MIFARE_NFC_DISABLED;
            else
            {
                var filters = Actions.Select(s => new IntentFilter(s)).ToArray();
                var intent = new Intent(_act, _act.GetType()).AddFlags(ActivityFlags.SingleTop);
                var pendingIntent = PendingIntent.GetActivity(_act, 0, intent, 0);
                NfcAdapter.EnableForegroundDispatch(_act, pendingIntent, filters, null);
                return MifareMessage.MIFARE_NFC_NO_ERROR;
            }
        }
        public MifareMessage OnPause()
        {
            if (!Init) return MifareMessage.MIFARE_NFC_NOT_INITIALIZED;
            else if (NfcAdapter == null) return MifareMessage.MIFARE_NFC_NOT_AVAILABLE;
            else if (!NfcAdapter.IsEnabled) return MifareMessage.MIFARE_NFC_DISABLED;
            else
            {
                NfcAdapter.DisableForegroundDispatch(_act);
                return MifareMessage.MIFARE_NFC_NO_ERROR;
            }
        }
        public MifareMessage OnNewIntent(Intent intent)
        {
            if (intent == null) return MifareMessage.MIFARE_NFC_NULL_INTENT;
            if (intent.Extras.IsEmpty) return MifareMessage.MIFARE_NFC_EMPTY_INTENT;
            if (Actions.Contains(intent.Action))
            {
                var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                if (tag == null) return MifareMessage.MIFARE_NFC_NULL_TAG;

                TagInfo? tagInfo;
                MifareMessage msg;
                (Mifare, tagInfo, msg) = ReadInfo(tag);
                if (msg != MifareMessage.MIFARE_NFC_NO_ERROR) return msg;

                OnNewTagDiscovered?.Invoke(tagInfo);
                if (AutoHandleWriting)
                {
                    if (_writingBolck < 0) return MifareMessage.MIFARE_NFC_AUTO_WRITE_NOT_SETUPED;
                    if (_writingData == null || (_writingData.Count() > 0 && _writingData.Count() <= 16)) return MifareMessage.MIFARE_NFC_INVALID_AUTO_WRITE_DATA;
                    var res = WriteDataToBlock(_writingBolck, _writingData);
                    if (res != MifareMessage.MIFARE_NFC_TAG_WRITTEN) return res;
                }
                else if (AutoHandleReading)
                {
                    if (_readingBolck < 0) return MifareMessage.MIFARE_NFC_AUTO_READ_NOT_SETUPED;
                    ReadDataFromBlock(_readingBolck);
                }
                return MifareMessage.MIFARE_NFC_NO_ERROR;
            }
            else return MifareMessage.MIFARE_NFC_INTENT_ACTION_NOT_FOUND;
        }
        public void SetAuthenticateKey(byte[] key)
        {
            AuthKey = key;
        }
        public (MifareClassic, TagInfo?, MifareMessage) ReadInfo(Tag tag)
        {
            MifareClassic mifc = MifareClassic.Get(tag);
            if (tag == null) return (null, null, MifareMessage.MIFARE_NFC_NULL_TAG);
            mifc.ConnectAsync().Wait();
            return (mifc, new TagInfo { Uid = tag.GetId(), BlockCount = mifc.BlockCount, SectorCount = mifc.SectorCount, Size = mifc.Size, Type = mifc.Type, TechList = tag.GetTechList().ToList() }, MifareMessage.MIFARE_NFC_NO_ERROR);
        }
        public MifareMessage AuthenticateSector(MifareClassic mifc, int sector, byte[] key)
        {
            var tmp = key;
            MifareClassic.KeyDefault.CopyTo(tmp, 0);
            if (Task.Factory.StartNew<bool>(() => mifc.AuthenticateSectorWithKeyA(1, key)).Result)
            {
                tmp = key;
                MifareClassic.KeyDefault.CopyTo(tmp, 0);
                if (Task.Factory.StartNew<bool>(() => mifc.AuthenticateSectorWithKeyB(1, key)).Result)
                {
                    return MifareMessage.MIFARE_NFC_AUTH_OK;
                }
                else return MifareMessage.MIFARE_NFC_AUTH_A_FAILED;
            }
            else return MifareMessage.MIFARE_NFC_AUTH_A_FAILED;
        }
        public List<byte[]> ReadDataFromSector(MifareClassic mifc, int sector)
        {
            int firstBlock = mifc.SectorToBlock(sector);
            int lastBlock = firstBlock + 4;
            List<byte[]> lstBlocks = new List<byte[]>();
            for (int i = firstBlock; i < lastBlock; i++)
            {
                byte[] block = mifc.ReadBlockAsync(i).Result;
                lstBlocks.Add(block);
            }
            return lstBlocks;
        }
        public byte[] ReadDataFromBlock(MifareClassic mifc, int block)
        {
            var res = mifc.ReadBlockAsync(block).Result;
            OnReadingBlock(block, res);
            return res;
        }
        public MifareMessage WriteDataToBlock(MifareClassic mifc, int block, byte[] data)
        {
            Task.Factory.StartNew(() => mifc.WriteBlockAsync(block, data).Wait());
            OnWritingBlock(block, data);
            return MifareMessage.MIFARE_NFC_TAG_WRITTEN;
        }
        public MifareMessage WriteDataToBlock_WhenTagDetected(int block, byte[] data, bool ignoreLastIncompleteWrite = false)
        {
            if (!AutoHandleWriting) return MifareMessage.MIFARE_NFC_AUTO_WRITE_DISABLED;
            if (_waitingForWrite && !ignoreLastIncompleteWrite) return MifareMessage.MIFARE_NFC_LAST_WRITE_INCOMPLETE;
            _waitingForWrite = true;
            _writingBolck = block;
            _writingData = data;
            return MifareMessage.MIFARE_NFC_WAITING_FOR_TAG;
        }
        public MifareMessage ReadDataFromBlock_WhenTagDetected(int block)
        {
            if (!AutoHandleReading) return MifareMessage.MIFARE_NFC_AUTO_READ_DISABLED;
            _readingBolck = block;
            return MifareMessage.MIFARE_NFC_WAITING_FOR_TAG;
        }
        public string CheckIntentActions(string[] actions)
        {
            var allact = new string[] { NfcAdapter.ActionAdapterStateChanged, NfcAdapter.ActionNdefDiscovered, NfcAdapter.ActionTagDiscovered, NfcAdapter.ActionTechDiscovered, NfcAdapter.ActionTransactionDetected };
            string s = "";
            foreach (var item in actions)
            {
                if (!allact.Contains(item)) s += item + " || ";
            }
            return s;
        }
        #endregion
        #region Private Methods
        private MifareMessage AuthenticateSector(int sector, byte[] key)
        {
            return AuthenticateSector(Mifare, sector, key);
        }
        private List<byte[]> ReadDataFromSector(int sector)
        {
            return ReadDataFromSector(Mifare, sector);
        }
        private byte[] ReadDataFromBlock(int block)
        {
            return ReadDataFromBlock(Mifare, block);
        }
        private MifareMessage WriteDataToBlock(int block, byte[] data)
        {
            return WriteDataToBlock(Mifare, block, data);
        }
        #endregion

        public enum MifareMessage : uint
        {
            MIFARE_NFC_NO_ERROR = 1,
            MIFARE_NFC_NULL_CONTEXT = 2,
            MIFARE_NFC_NOT_INITIALIZED = 4,
            MIFARE_NFC_NOT_AVAILABLE = 8,
            MIFARE_NFC_DISABLED = 16,
            MIFARE_NFC_NULL_INTENT = 32,
            MIFARE_NFC_EMPTY_INTENT = 64,
            MIFARE_NFC_INVALID_INTENT_ACTION = 128,
            MIFARE_NFC_INTENT_ACTION_NOT_FOUND = 256,
            MIFARE_NFC_NULL_TAG = 512,
            MIFARE_NFC_AUTH_A_FAILED = 1024,
            MIFARE_NFC_AUTH_B_FAILED = 2048,
            MIFARE_NFC_AUTH_OK = 4096,
            MIFARE_NFC_TAG_WRITTEN = 8192,
            MIFARE_NFC_AUTO_WRITE_DISABLED = 16384,
            MIFARE_NFC_LAST_WRITE_INCOMPLETE = 32768,
            MIFARE_NFC_WAITING_FOR_TAG = 65536,
            MIFARE_NFC_AUTO_READ_DISABLED = 131072,
            MIFARE_NFC_AUTO_WRITE_NOT_SETUPED = 262144,
            MIFARE_NFC_AUTO_READ_NOT_SETUPED = 524288,
            MIFARE_NFC_INVALID_AUTO_WRITE_DATA = 1048576,
        }
        public struct TagInfo
        {
            public byte[] Uid { get; set; }
            public int BlockCount { get; set; }
            public int SectorCount { get; set; }
            public int Size { get; set; }
            public MifareClassicType Type { get; set; }
            public List<string> TechList { get; set; }
        }
    }
}