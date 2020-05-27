using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Nfc;
using Android.Nfc.Tech;
using Android.Speech.Tts;
using Android.Widget;

namespace MifareNFCLib
{
    public class NFC
    {
        #region Const
        public const string Tech_IsoDep = "android.nfc.tech.IsoDep";
        public const string Tech_MifareClassic = "android.nfc.tech.MifareClassic";
        public const string Tech_MifareUltralight = "android.nfc.tech.MifareUltralight";
        public const string Tech_Ndef = "android.nfc.tech.Ndef";
        public const string Tech_NdefFormatable = "android.nfc.tech.";
        public const string Tech_NfcA = "android.nfc.tech.NfcA";
        public const string Tech_NfcB = "android.nfc.tech.NfcB";
        public const string Tech_NfcBarcode = "android.nfc.tech.NfcBarcode";
        public const string Tech_NfcF = "android.nfc.tech.NfcF";
        public const string Tech_NfcV = "android.nfc.tech.NfcV";
        #endregion
        #region Public Members
        public NfcAdapter NfcAdapter { get; private set; }
        public List<string> Actions { get; private set; }

        public Action<TagInfo?> OnNewTagDiscovered;
        public Action<NdefMessage> OnReading_NdefMessage;
        public Action<NdefMessage> OnWriting_NdefMessage;
        public Action OnFormatting_NdefTag;
        #endregion
        #region Private Members
        private Activity _act;
        private bool Init = false;
        #endregion
        #region Public Methods
        public NFCMessage Initialize(Activity act, string[] actions)
        {
            if (act == null) return NFCMessage.NFC_NULL_CONTEXT;
            _act = act;
            NfcAdapter = NfcAdapter.GetDefaultAdapter(act);
            Init = true;
            if (actions == null || actions.Count() == 0)
            {
                Actions = new List<string> { NfcAdapter.ActionTagDiscovered, NfcAdapter.ActionNdefDiscovered, NfcAdapter.ActionTechDiscovered };
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(CheckIntentActions(actions))) return NFCMessage.NFC_INVALID_INTENT_ACTION;
                Actions = actions.ToList();
            }
            if (NfcAdapter == null) return NFCMessage.NFC_NOT_AVAILABLE;
            if (!NfcAdapter.IsEnabled) return NFCMessage.NFC_DISABLED;
            return NFCMessage.NFC_NO_ERROR;
        }
        public NFCMessage Initialize(Activity act)
        {
            return Initialize(act, null);
        }
        public NFCMessage OnResume()
        {
            if (!Init) return NFCMessage.NFC_NOT_INITIALIZED;
            else if (NfcAdapter == null) return NFCMessage.NFC_NOT_AVAILABLE;
            else if (!NfcAdapter.IsEnabled) return NFCMessage.NFC_DISABLED;
            else
            {
                var filters = Actions.Select(s => new IntentFilter(s)).ToArray();
                var intent = new Intent(_act, _act.GetType()).AddFlags(ActivityFlags.SingleTop);
                var pendingIntent = PendingIntent.GetActivity(_act, 0, intent, 0);
                NfcAdapter.EnableForegroundDispatch(_act, pendingIntent, filters, null);
                return NFCMessage.NFC_NO_ERROR;
            }
        }
        public NFCMessage OnPause()
        {
            if (!Init) return NFCMessage.NFC_NOT_INITIALIZED;
            else if (NfcAdapter == null) return NFCMessage.NFC_NOT_AVAILABLE;
            else if (!NfcAdapter.IsEnabled) return NFCMessage.NFC_DISABLED;
            else
            {
                NfcAdapter.DisableForegroundDispatch(_act);
                return NFCMessage.NFC_NO_ERROR;
            }
        }
        public NFCMessage OnNewIntent(Intent intent)
        {
            if (intent == null) return NFCMessage.NFC_NULL_INTENT;
            if (intent.Extras.IsEmpty) return NFCMessage.NFC_EMPTY_INTENT;
            if (Actions.Contains(intent.Action))
            {
                Tag tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
                if (tag == null) return NFCMessage.NFC_NULL_TAG;
                TagInfo? tagInfo;
                NFCMessage msg;
                (tagInfo, msg) = ReadInfo(tag);
                if (msg != NFCMessage.NFC_NO_ERROR) return msg;
                OnNewTagDiscovered?.Invoke(tagInfo);
                return NFCMessage.NFC_NO_ERROR;
            }
            else return NFCMessage.NFC_INTENT_ACTION_NOT_FOUND;
        }
        public (TagInfo?, NFCMessage) ReadInfo(Tag tag)
        {
            if (tag == null) return (null, NFCMessage.NFC_NULL_TAG);
            return (new TagInfo
            {
                Uid = tag.GetId(),
                IsoDep = IsoDep.Get(tag),
                MifareClassic = MifareClassic.Get(tag),
                MifareUltralight = MifareUltralight.Get(tag),
                NfcV = NfcV.Get(tag),
                Ndef = Ndef.Get(tag),
                NdefFormatable = NdefFormatable.Get(tag),
                NfcA = NfcA.Get(tag),
                NfcB = NfcB.Get(tag),
                NfcF = NfcF.Get(tag),
                NfcBarcode = NfcBarcode.Get(tag),
                TechList = tag.GetTechList()
            }, NFCMessage.NFC_NO_ERROR);
        }
        public NFCMessage MifareClassic_AuthenticateSectorWithKeyA(MifareClassic mfc, int Sector, List<byte[]> Keys)
        {
            if (mfc == null) return NFCMessage.NFC_NULL_MIFARECLASSIC;
            if (!mfc.IsConnected) mfc.Connect();
            bool auth = false;
            int i = 0;
            if (Keys == null || Keys.Count == 0)
            {
                auth = mfc.AuthenticateSectorWithKeyA(Sector, MifareClassic.KeyNfcForum.ToArray());
                if (!auth) auth = mfc.AuthenticateSectorWithKeyA(Sector, MifareClassic.KeyDefault.ToArray());
            }
            else
            {
                while (!auth || i != Keys.Count)
                {
                    auth = mfc.AuthenticateSectorWithKeyA(Sector, Keys[i]);
                    i++;
                }
            }
            return auth ? NFCMessage.NFC_AUTH_OK : NFCMessage.NFC_AUTH_FAIELD;
        }
        public NFCMessage MifareClassic_AuthenticateSectorWithKeyB(MifareClassic mfc, int Sector, List<byte[]> Keys)
        {
            if (mfc == null) return NFCMessage.NFC_NULL_MIFARECLASSIC;
            if (!mfc.IsConnected) mfc.Connect();
            bool auth = false;
            int i = 0;
            if (Keys == null || Keys.Count == 0)
            {
                auth = mfc.AuthenticateSectorWithKeyB(Sector, MifareClassic.KeyNfcForum.ToArray());
                if (!auth) auth = mfc.AuthenticateSectorWithKeyB(Sector, MifareClassic.KeyDefault.ToArray());
            }
            else
            {
                while (!auth || i != Keys.Count)
                {
                    auth = mfc.AuthenticateSectorWithKeyB(Sector, Keys[i]);
                    i++;
                }
            }
            return auth ? NFCMessage.NFC_AUTH_OK : NFCMessage.NFC_AUTH_FAIELD;
        }
        public List<byte[]> MifareClassic_ReadSector(MifareClassic mfc, int Sector)
        {
            int blockCount = mfc.GetBlockCountInSector(Sector);
            var lst = new List<byte[]>(blockCount);
            int readIndex = mfc.SectorToBlock(1);
            for (int i = 0; i < blockCount; i++)
            {
                lst[i] = mfc.ReadBlock(readIndex);
                readIndex++;
            }
            return lst;
        }
        public byte[] MifareClassic_ReadBlock(MifareClassic mfc, int Block)
        {
            return mfc.ReadBlock(Block);
        }
        public void MifareClassic_WriteBlock(MifareClassic mfc, int Block,byte[] Data)
        {
            var tmp = new byte[16];
            if (Data.Length >= 16)
            {
                for (int i = 0; i < 16; i++)
                {
                    tmp[i] = Data[i];
                }
            }
            else
            {
                for (int i = 0; i < Data.Length; i++)
                {
                    tmp[i] = Data[i];
                }
                for (int i = Data.Length; i < 16; i++)
                {
                    tmp[i] = (byte)' ';
                }
            }
            mfc.WriteBlock(Block, tmp);
        }
        public NdefMessage Ndef_ReadMessage(Ndef ndf)
        {
            var res = ndf.NdefMessage;
            OnReading_NdefMessage?.Invoke(res);
            return res;
        }
        public NFCMessage Ndef_WriteMessage(Ndef ndf, NdefMessage msg)
        {
            if (!ndf.IsWritable)
            {
                return NFCMessage.NFC_UN_WRITABLE_TAG;
            }
            if (ndf.MaxSize < msg.ToByteArray().Length)
            {
                return NFCMessage.NFC_INVALID_MSG;
            }
            ndf.WriteNdefMessage(msg);
            OnWriting_NdefMessage?.Invoke(msg);
            return NFCMessage.NFC_TAG_WRITTEN;
        }
        public NFCMessage NdefFormatable_FormatTag(Tag tag)
        {
            if (!tag.GetTechList().Contains(Tech_NdefFormatable)) return NFCMessage.NFC_UN_FORMATABLE_TAG;
            NdefFormatable ndefFormatable = NdefFormatable.Get(tag);
            if (ndefFormatable == null) return NFCMessage.NFC_CANT_FORMAT;
            ndefFormatable.Connect();
            NdefRecord record = NdefRecord.CreateMime("text/plain", Encoding.ASCII.GetBytes("New"));
            NdefMessage message = new NdefMessage(new NdefRecord[] { record });
            ndefFormatable.Format(message);
            ndefFormatable.Close();
            OnFormatting_NdefTag?.Invoke();
            return NFCMessage.NFC_TAG_FORMATED;
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
        public enum NFCMessage : uint
        {
            NFC_NO_ERROR = 1,
            NFC_NULL_CONTEXT = 2,
            NFC_NOT_INITIALIZED = 4,
            NFC_NOT_AVAILABLE = 8,
            NFC_DISABLED = 16,
            NFC_NULL_INTENT = 32,
            NFC_EMPTY_INTENT = 64,
            NFC_INVALID_INTENT_ACTION = 128,
            NFC_INTENT_ACTION_NOT_FOUND = 256,
            NFC_NULL_TAG = 512,
            NFC_UN_WRITABLE_TAG = 1024,
            NFC_INVALID_MSG = 2048,
            NFC_AUTH_OK = 4096,
            NFC_AUTH_FAIELD,
            NFC_TAG_WRITTEN = 8192,
            NFC_AUTO_WRITE_DISABLED = 16384,
            NFC_LAST_WRITE_INCOMPLETE = 32768,
            NFC_WAITING_FOR_TAG = 65536,
            NFC_AUTO_WRITE_NOT_SETUPED = 131072,
            NFC_INVALID_AUTO_WRITE_DATA = 262144,
            NFC_TAG_FORMATED,
            NFC_UN_FORMATABLE_TAG,
            NFC_CANT_FORMAT,
            NFC_NULL_NDEF,
            NFC_NULL_ISODEP,
            NFC_NULL_MIFARECLASSIC,
            NFC_NULL_MIFAREULTRALIGHT,
            NFC_NULL_NDEFFORMATABLE,
            NFC_NULL_NFCA,
            NFC_NULL_NFCB,
            NFC_NULL_NFCF,
            NFC_NULL_NFCV,
            NFC_NULL_NFCBARCODE,
        }
        public struct TagInfo
        {
            public byte[] Uid { get; set; }
            public IsoDep IsoDep { get; set; }
            public MifareClassic MifareClassic { get; set; }
            public MifareUltralight MifareUltralight { get; set; }
            public Ndef Ndef { get; set; }
            public NdefFormatable NdefFormatable { get; set; }
            public NfcA NfcA { get; set; }
            public NfcB NfcB { get; set; }
            public NfcBarcode NfcBarcode { get; set; }
            public NfcF NfcF { get; set; }
            public NfcV NfcV { get; set; }
            public string[] TechList { get; set; }
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
                return $"[UID : {UID()}]";
            }
        }
    }
}