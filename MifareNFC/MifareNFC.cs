using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;

namespace MifareNFCLib
{
    public class MifareNFC
    {
        #region Public Members
        public NfcAdapter NfcAdapter { get; private set; }
        public List<string> Actions { get; private set; }
        public byte[] AuthKey { get; set; } = MifareClassic.KeyDefault.ToArray();
        public Ndef Mifare { get; private set; }
        public bool AutoHandleWriting { get; set; } = true;
        public bool AutoHandleReading { get; set; } = true;

        public Action<TagInfo?> OnNewTagDiscovered;
        public Action<NdefMessage> OnReadingMessage;
        public Action<NdefMessage> OnWritingMessage;
        #endregion
        #region Private Members
        private Activity _act;
        private bool Init = false;
        private bool _waitingForWrite = false;
        private NdefMessage _writingMsg;
        #endregion
        #region Public Methods
        public MifareMessage Initialize(Activity act, string[] actions)
        {
            if (act == null) return MifareMessage.MIFARE_NFC_NULL_CONTEXT;
            _act = act;
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
        public MifareMessage Initialize(Activity act)
        {
            return Initialize(act, null);
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
                Tag tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                if (tag == null) return MifareMessage.MIFARE_NFC_NULL_TAG;

                TagInfo? tagInfo;
                MifareMessage msg;
                (Mifare, tagInfo, msg) = ReadInfo(tag);
                if (msg != MifareMessage.MIFARE_NFC_NO_ERROR) return msg;

                OnNewTagDiscovered?.Invoke(tagInfo);
                if (AutoHandleWriting && _waitingForWrite)
                {
                    if (_writingMsg == null) return MifareMessage.MIFARE_NFC_INVALID_AUTO_WRITE_DATA;
                    var res = WriteMessage(_writingMsg);
                    _waitingForWrite = false;
                    _writingMsg = null;
                    if (res != MifareMessage.MIFARE_NFC_TAG_WRITTEN) return res;
                }
                return MifareMessage.MIFARE_NFC_NO_ERROR;
            }
            else return MifareMessage.MIFARE_NFC_INTENT_ACTION_NOT_FOUND;
        }
        //public void SetAuthenticateKey(byte[] key)
        //{
        //    AuthKey = key;
        //}
        //public MifareMessage AuthenticateSector(Ndef mifc, int sector, byte[] key)
        //{

        //    mifc.Connect();
        //    bool authA = mifc.AuthenticateSectorWithKeyA(2, Ndef.KeyNfcForum.ToArray());
        //    bool authB = mifc.AuthenticateSectorWithKeyB(2, Ndef.KeyDefault.ToArray());
        //    if (authA && authB) return MifareMessage.MIFARE_NFC_AUTH_OK;
        //    if (!authA) return MifareMessage.MIFARE_NFC_AUTH_A_FAILED;
        //    if (!authB) return MifareMessage.MIFARE_NFC_AUTH_B_FAILED;
        //    return MifareMessage.MIFARE_NFC_AUTH_A_FAILED;
        //    //var tmp = key;
        //    //Ndef.KeyDefault.CopyTo(tmp, 0);
        //    //if (Task.Factory.StartNew<bool>(() => mifc.AuthenticateSectorWithKeyA(1, key)).Result)
        //    //{
        //    //    tmp = key;
        //    //    Ndef.KeyDefault.CopyTo(tmp, 0);
        //    //    if (Task.Factory.StartNew<bool>(() => mifc.AuthenticateSectorWithKeyB(1, key)).Result)
        //    //    {
        //    //        return MifareMessage.MIFARE_NFC_AUTH_OK;
        //    //    }
        //    //    else return MifareMessage.MIFARE_NFC_AUTH_A_FAILED;
        //    //}
        //    //else return MifareMessage.MIFARE_NFC_AUTH_A_FAILED;
        //}
        public (Ndef, TagInfo?, MifareMessage) ReadInfo(Tag tag)
        {
            Ndef mifc = Ndef.Get(tag);
            if (mifc == null) return (null, null, MifareMessage.MIFARE_NFC_NULL_TAG);
            mifc.Connect();
            var msg = ReadMessage(mifc);
            return (mifc, new TagInfo { Uid = tag.GetId(), MaxSize = mifc.MaxSize, Type = mifc.Type, NdefMessage = msg, Size = msg.ToByteArray().Length, TechList = tag.GetTechList().ToList() }, MifareMessage.MIFARE_NFC_NO_ERROR);
        }
        public NdefMessage ReadMessage(Ndef mifc)
        {
            var res = mifc.NdefMessage;
            OnReadingMessage(res);
            return res;
        }
        public MifareMessage WriteMessage(Ndef mifc, NdefMessage msg)
        {
            if (!mifc.IsWritable)
            {
                return MifareMessage.MIFARE_NFC_UN_WRITABLE_TAG;
            }
            if (mifc.MaxSize < msg.ToByteArray().Length)
            {
                return MifareMessage.MIFARE_NFC_INVALID_MSG;
            }
            mifc.WriteNdefMessage(msg);
            return MifareMessage.MIFARE_NFC_TAG_WRITTEN;
        }
        public MifareMessage WriteDataToBlock_WhenTagDetected(NdefMessage msg, bool ignoreLastIncompleteWrite = false)
        {
            if (!AutoHandleWriting) return MifareMessage.MIFARE_NFC_AUTO_WRITE_DISABLED;
            if (_waitingForWrite && !ignoreLastIncompleteWrite) return MifareMessage.MIFARE_NFC_LAST_WRITE_INCOMPLETE;
            _waitingForWrite = true;
            _writingMsg = msg;
            return MifareMessage.MIFARE_NFC_WAITING_FOR_TAG;
        }
        public void Enable_WriteDataToBlock_WhenTagDetected()
        {
            AutoHandleWriting = true;
        }
        public void Disable_WriteDataToBlock_WhenTagDetected()
        {
            AutoHandleWriting = false;
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
        //private MifareMessage AuthenticateSector(int sector, byte[] key)
        //{
        //    return AuthenticateSector(Mifare, sector, key);
        //}
        private NdefMessage ReadMessage()
        {
            return ReadMessage(Mifare);
        }
        private MifareMessage WriteMessage(NdefMessage msg)
        {
            return WriteMessage(Mifare, msg);
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
            MIFARE_NFC_UN_WRITABLE_TAG = 1024,
            MIFARE_NFC_INVALID_MSG = 2048,
            MIFARE_NFC_AUTH_OK = 4096,
            MIFARE_NFC_TAG_WRITTEN = 8192,
            MIFARE_NFC_AUTO_WRITE_DISABLED = 16384,
            MIFARE_NFC_LAST_WRITE_INCOMPLETE = 32768,
            MIFARE_NFC_WAITING_FOR_TAG = 65536,
            MIFARE_NFC_AUTO_WRITE_NOT_SETUPED = 131072,
            MIFARE_NFC_INVALID_AUTO_WRITE_DATA = 262144,
        }
        public struct TagInfo
        {
            public byte[] Uid { get; set; }
            public int MaxSize { get; set; }
            public int Size { get; set; }
            public string Type { get; set; }
            public bool IsWritable { get; set; }
            public List<string> TechList { get; set; }
            public NdefMessage NdefMessage { get; set; }
            public string UID()
            {
                string data = "";
                for (int ii = 0; ii < Uid.Length; ii++)
                {
                    if (!string.IsNullOrEmpty(data))
                        data += "-";
                    data += Uid[ii].ToString("X2");
                }
                return data;
            }
            public override string ToString()
            {
                return $"[UID : {UID()}] [Max Size : {MaxSize}] [Size : {Size}] [Is Writable : {IsWritable}] [Type : {Type}]";
            }
        }
    }
}