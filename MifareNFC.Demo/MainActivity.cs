using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Support.Design.Widget;
using Android.Content;
using System.Text;
using Android.Nfc.Tech;
using Java.Lang.Reflect;
using System.Collections.Generic;
using System.Linq;
using Android.Nfc;
using System.IO;
using System.Runtime.CompilerServices;
using System;
using MifareNFCLib.Demo.ListView;

namespace MifareNFCLib.Demo
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        //private EditText CLI;
        private EditText TxtMsg;
        private TextView TxtState;
        private readonly NFC NFC = new NFC();
        private Android.Support.V7.App.AlertDialog.Builder wait;
        private WaitingState State;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            //CLI = FindViewById<EditText>(Resource.Id.editText1);
            FindViewById<Button>(Resource.Id.wndef).Click += WriteNdef_Click;
            FindViewById<Button>(Resource.Id.wmifare).Click += WriteMifare_Click;
            FindViewById<Button>(Resource.Id.nformat).Click += Format_Click;
            //FindViewById<Button>(Resource.Id.lgcle).Click += Clearlog_Click;
            TxtMsg = FindViewById<EditText>(Resource.Id.wmsg);
            TxtState = FindViewById<TextView>(Resource.Id.txtstate);
            //Terminal.Init(CLI);

            Terminal.WriteLine("Initializeing...");
            Terminal.WriteLine(NFC.Initialize(this, new string[] { NfcAdapter.ActionTagDiscovered }).ToString());
            NFC.OnNewTagDiscovered += NewTagDetected;
            //NFC.OnReading_NdefMessage += OnReadNdefMessage;
            //NFC.OnWriting_NdefMessage += OnWriteNdefMessage;
            TxtState.Text = "Initialized";
            var item = new List<IListItem>();
            item.Add(new StatusHeaderListItem("Waiting For Tag..."));
            //item.Add(new DataItem("hi", "text"));
            var lst = FindViewById<Android.Widget.ListView>(Resource.Id.lstview);
            lst.Adapter = new ListViewAdapter(this, item);
        }
        private void Clearlog_Click(object sender, EventArgs e)
        {
            Terminal.Clear();
        }
        void Openwait()
        {
            wait = new Android.Support.V7.App.AlertDialog.Builder(this);
            wait.SetTitle("Waiting For Tag...");
            wait.SetMessage("Tap Your Tag To Complete Action...");
            wait.Create().Show();
        }
        private void WriteNdef_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMsg.Text))
            {
                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                msg.SetTitle("Empty Message!!!");
                msg.SetMessage("Please Enter Message To Write...");
                msg.SetPositiveButton("OK", (s, a) => { });
                msg.SetCancelable(true);
                msg.Create().Show();
                return;
            }
            Openwait();
            TxtState.Text = "Ndef - Waiting For Tag";
            Terminal.WriteLine($"Set Ndef Writing Data > {TxtMsg.Text}");
            State = WaitingState.Write_Ndef;
        }
        private void WriteMifare_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMsg.Text))
            {
                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                msg.SetTitle("Empty Message!!!");
                msg.SetMessage("Please Enter Message To Write...");
                msg.SetPositiveButton("OK", (s, a) => { });
                msg.SetCancelable(true);
                msg.Create().Show();
                return;
            }
            Openwait();
            TxtState.Text = "Mifare - Waiting For Tag";
            Terminal.WriteLine($"Set Mifare Writing Data > {TxtMsg.Text}");
            State = WaitingState.Write_Mifare;
        }
        private void Format_Click(object sender, System.EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtMsg.Text))
            {
                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                msg.SetTitle("Empty Message!!!");
                msg.SetMessage("Please Enter First Message To Write...");
                msg.SetPositiveButton("OK", (s, a) => { });
                msg.SetCancelable(true);
                msg.Create().Show();
                return;
            }
            Openwait();
            TxtState.Text = "Format - Waiting For Tag";
            Terminal.WriteLine($"Set Ndef Format Data");
            State = WaitingState.Format_Ndef;
        }

        public void AddItem(DataItem dataItem)
        {
            var lst = FindViewById<Android.Widget.ListView>(Resource.Id.lstview);
            var adapter = lst.Adapter as ListViewAdapter;

            var items = adapter?.Items.Where(m => m.GetListItemType() == ListItemType.DataItem).ToList();

            if (items != null && !items.Any(x =>
                    ((DataItem)x).Text == dataItem.Text && ((DataItem)x).SubTitle == dataItem.SubTitle))
            {
                adapter.Items.Add(dataItem);
            }

            lst.Adapter = new ListViewAdapter(this, adapter?.Items);
        }
        public void ClearItems()
        {
            var lst = FindViewById<Android.Widget.ListView>(Resource.Id.lstview);
            var adapter = lst.Adapter as ListViewAdapter;
            adapter?.Items.Clear();
            lst.Adapter = new ListViewAdapter(this, adapter?.Items);
        }
        public void AddHeader(HeaderListItem dataItem)
        {
            var lst = FindViewById<Android.Widget.ListView>(Resource.Id.lstview);
            var adapter = lst.Adapter as ListViewAdapter;

            var items = adapter?.Items.Where(m => m.GetListItemType() == ListItemType.Header).ToList();

            if (items != null && !items.Any(x => ((HeaderListItem)x).Text == dataItem.Text))
            {
                adapter.Items.Add(dataItem);
            }

            lst.Adapter = new ListViewAdapter(this, adapter?.Items);
        }
        public void UpdateStatus(string discoveryStatus)
        {
            var lst = FindViewById<Android.Widget.ListView>(Resource.Id.lstview);
            var adapter = lst.Adapter as ListViewAdapter;

            var hasStatusItem = adapter?.Items?.Any(m => m.GetListItemType() == ListItemType.Status);

            if (hasStatusItem.HasValue && hasStatusItem.Value)
            {
                var statusItem = adapter.Items.Single(m => m.GetListItemType() == ListItemType.Status);
                statusItem.Text = discoveryStatus;
            }

            lst.Adapter = new ListViewAdapter(this, adapter?.Items);
        }
        protected override void OnResume()
        {
            base.OnResume();
            NFC.OnResume();
        }
        protected override void OnPause()
        {
            base.OnPause();
            //NFC.OnPause();
            //disableTagWriteMode();

        }
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Terminal.WriteLine("New Intent >> " + NFC.OnNewIntent(intent).ToString());

        }

        void NewTagDetected(NFC.TagInfo? info)
        {
            ClearItems();
            var item = new List<IListItem>();
            item.Add(new StatusHeaderListItem("Waiting For Tag..."));
            //item.Add(new DataItem("hi", "text"));
            var lst = FindViewById<Android.Widget.ListView>(Resource.Id.lstview);
            lst.Adapter = new ListViewAdapter(this, item);
            UpdateStatus("New Tag Detected!");
            Terminal.WriteLine("New Tag Detected!");
            if (info == null) { Terminal.WriteLine("Tag Info Returned Null"); UpdateStatus("Tag Info Returned Null!"); return; }
            Terminal.WriteLine("------------------------------------------------------------");
            string Tech = " | ";
            foreach (var tec in info.Value.TechList) Tech += tec.Replace("android.nfc.tech.", "") + " | ";
            Terminal.WriteLine("TechList > " + Tech);
            AddHeader(new HeaderListItem("Tag Info"));
            AddItem(new DataItem("Serial Number", info.Value.UID()));
            AddItem(new DataItem("Tech List", Tech));
            foreach (var tec in info.Value.TechList)
            {
                switch (tec)
                {
                    case NFC.Tech_IsoDep:
                        if (info.Value.IsoDep == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Max Transcive Length", $"{info.Value.IsoDep.MaxTransceiveLength} bytes"));
                        AddItem(new DataItem("Timeout", $"{info.Value.IsoDep.Timeout} ms"));
                        break;

                    case NFC.Tech_MifareClassic:
                        if (info.Value.MifareClassic == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Type", $"{info.Value.MifareClassic.Type}"));
                        AddItem(new DataItem("Size", $"{info.Value.MifareClassic.Size} bytes"));
                        AddItem(new DataItem("Block Count", $"{info.Value.MifareClassic.BlockCount} blocks"));
                        AddItem(new DataItem("Sector Count", $"{info.Value.MifareClassic.SectorCount} sectors"));
                        AddItem(new DataItem("Max Transcive Length", $"{info.Value.MifareClassic.MaxTransceiveLength} bytes"));
                        AddItem(new DataItem("Timeout", $"{info.Value.MifareClassic.Timeout} ms"));
                        info.Value.MifareClassic.Connect();
                        var res = NFC.MifareClassic_AuthenticateSectorWithKeyA(info.Value.MifareClassic, 1, null);
                        Terminal.WriteLine($"Auth > {res}");
                        if (State == WaitingState.Write_Mifare)
                        {
                            if (res == NFC.NFCMessage.NFC_AUTH_OK)
                            {
                                Terminal.WriteLine($"Writing Mifare > {TxtMsg.Text}");
                                NFC.MifareClassic_WriteBlock(info.Value.MifareClassic, info.Value.MifareClassic.SectorToBlock(1), Encoding.UTF8.GetBytes(TxtMsg.Text));
                                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                                msg.SetTitle("Success!!!");
                                msg.SetMessage("Tag Written Successfully...");
                                msg.SetPositiveButton("OK", (s, a) => { });
                                msg.SetCancelable(true);
                                msg.Create().Show();
                                TxtState.Text = "Written Mifare";
                            }
                            else
                            {
                                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                                msg.SetTitle("Error!!!");
                                msg.SetMessage("Failed To Write Tag...");
                                msg.SetPositiveButton("OK", (s, a) => { });
                                msg.SetCancelable(true);
                                msg.Create().Show();
                                TxtState.Text = "Failed Write Mifare";
                            }
                        }
                        if (State == WaitingState.None)
                        {
                            if (res == NFC.NFCMessage.NFC_AUTH_OK)
                            {
                                var rm = NFC.MifareClassic_ReadBlock(info.Value.MifareClassic, info.Value.MifareClassic.SectorToBlock(1));
                                Terminal.WriteLine($"Reading Mifare > {Encoding.UTF8.GetString(rm)}");
                                AddItem(new DataItem("Reading", Encoding.UTF8.GetString(rm)));
                            }
                        }
                        info.Value.MifareClassic.Close();
                        break;

                    case NFC.Tech_MifareUltralight:
                        if (info.Value.MifareUltralight == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Type", $"{info.Value.MifareUltralight.Type}"));
                        AddItem(new DataItem("Max Transcive Length", $"{info.Value.MifareUltralight.MaxTransceiveLength} bytes"));
                        AddItem(new DataItem("Timeout", $"{info.Value.MifareUltralight.Timeout} ms"));
                        break;

                    case NFC.Tech_Ndef:
                        if (info.Value.Ndef == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Type", $"{info.Value.Ndef.Type}"));
                        AddItem(new DataItem("Writable", $"{info.Value.Ndef.IsWritable}"));
                        AddItem(new DataItem("Can Make Read-Only", $"{info.Value.Ndef.CanMakeReadOnly()}"));
                        AddItem(new DataItem("Max Size", $"{info.Value.Ndef.MaxSize} bytes"));
                        AddHeader(new HeaderListItem($"Ndef Message..."));
                        info.Value.Ndef.Connect();
                        if (State == WaitingState.Write_Ndef)
                        {
                            Terminal.WriteLine($"Writing Ndef > {TxtMsg.Text}");
                            NdefRecord record = NdefRecord.CreateMime("text/plain", Encoding.ASCII.GetBytes(TxtMsg.Text));
                            //NdefRecord record = NdefRecord.CreateTextRecord("", "SomeTxt");
                            NdefMessage message = new NdefMessage(new NdefRecord[] { record });
                            NFC.Ndef_WriteMessage(info.Value.Ndef, message);
                            Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                            msg.SetTitle("Success!!!");
                            msg.SetMessage("Tag Written Successfully...");
                            msg.SetPositiveButton("OK", (s, a) => { });
                            msg.SetCancelable(true);
                            msg.Create().Show();
                            TxtState.Text = "Written Ndef";
                        }
                        if (State == WaitingState.None)
                        {
                            int i = 0;
                            foreach (var msg in NFC.Ndef_ReadMessage(info.Value.Ndef).GetRecords())
                            {
                                Terminal.WriteLine($"Reading Ndef Msg [{i}] > {Encoding.ASCII.GetString(msg.GetPayload())}");
                                AddItem(new DataItem($"Record [{i}] - [UFT-8] [{ msg.ToMimeType()}]", $"{Encoding.UTF8.GetString(msg.GetPayload())}"));
                                i++;
                            }
                        }
                        info.Value.Ndef.Close();
                        break;

                    case NFC.Tech_NdefFormatable:
                        if (info.Value.NdefFormatable == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        if (State == WaitingState.Format_Ndef)
                        {
                            var res1 = NFC.NdefFormatable_FormatTag(info.Value.NdefFormatable.Tag);
                            Terminal.WriteLine($"Ndef Format > {res1}");
                            if (res1 == NFC.NFCMessage.NFC_TAG_FORMATED)
                            {
                                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                                msg.SetTitle("Success!!!");
                                msg.SetMessage("Tag Formatted Successfully...");
                                msg.SetPositiveButton("OK", (s, a) => { });
                                msg.SetCancelable(true);
                                msg.Create().Show();
                                TxtState.Text = "Formated Ndef";
                            }
                            else
                            {
                                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                                msg.SetTitle("Error!!!");
                                msg.SetMessage("Failed To Format Tag...");
                                msg.SetPositiveButton("OK", (s, a) => { });
                                msg.SetCancelable(true);
                                msg.Create().Show();
                                TxtState.Text = "Failed Format Ndef";
                            }
                        }
                        if (State == WaitingState.None)
                        {

                        }
                        break;

                    case NFC.Tech_NfcA:
                        if (info.Value.NfcA == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        info.Value.NfcA.Connect();
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Max Transcive Length", $"{info.Value.NfcA.MaxTransceiveLength} bytes"));
                        AddItem(new DataItem("Timeout", $"{info.Value.NfcA.Timeout} ms"));
                        AddItem(new DataItem("Sak", $"{info.Value.NfcA.Sak}"));
                        AddItem(new DataItem("Atqa", $"{Encoding.ASCII.GetString(info.Value.NfcA.GetAtqa())}"));
                        info.Value.NfcA.Close();
                        break;

                    case NFC.Tech_NfcB:
                        if (info.Value.NfcB == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        info.Value.NfcB.Connect();
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Max Transcive Length", $"{info.Value.NfcB.MaxTransceiveLength} bytes"));
                        AddItem(new DataItem("Application Data", $"{Encoding.ASCII.GetString(info.Value.NfcB.GetApplicationData())}"));
                        AddItem(new DataItem("Protocol Info", $"{Encoding.ASCII.GetString(info.Value.NfcB.GetProtocolInfo())}"));
                        info.Value.NfcB.Close();
                        break;

                    case NFC.Tech_NfcBarcode:
                        if (info.Value.NfcBarcode == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        info.Value.NfcBarcode.Connect();
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Type", $"{info.Value.NfcBarcode.Type}"));
                        AddItem(new DataItem("Barcode", $"{Encoding.ASCII.GetString(info.Value.NfcBarcode.GetBarcode())}"));
                        info.Value.NfcBarcode.Close();
                        break;

                    case NFC.Tech_NfcF:
                        if (info.Value.NfcF == null) return;
                        Terminal.WriteLine("\n");
                        Terminal.WriteLine($"Tag Tec = [{tec.Replace("android.nfc.tech.", "")}]");
                        info.Value.NfcF.Connect();
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Manufacturer", $"{Encoding.ASCII.GetString(info.Value.NfcF.GetManufacturer())}"));
                        AddItem(new DataItem("System Code", $"{Encoding.ASCII.GetString(info.Value.NfcF.GetSystemCode())}"));
                        AddItem(new DataItem("Max Transcive Length", $"{info.Value.NfcF.MaxTransceiveLength} bytes"));
                        AddItem(new DataItem("Timeout", $"{info.Value.NfcF.Timeout} ms"));
                        info.Value.NfcF.Close();
                        break;

                    case NFC.Tech_NfcV:
                        Terminal.WriteLine("\n");
                        if (info.Value.NfcV == null) return;
                        info.Value.NfcV.Connect();
                        AddHeader(new HeaderListItem($"Tech = [{tec.Replace("android.nfc.tech.", "")}]"));
                        AddItem(new DataItem("Dsf Id", $"{info.Value.NfcV.DsfId}"));
                        AddItem(new DataItem("Response Flags", $"{info.Value.NfcV.ResponseFlags}"));
                        AddItem(new DataItem("Max Transcive Length", $"{info.Value.NfcV.MaxTransceiveLength} bytes"));
                        info.Value.NfcV.Close();
                        break;
                }
            }
            if (TxtState.Text.Contains("Waiting"))
            {
                Android.Support.V7.App.AlertDialog.Builder msg = new Android.Support.V7.App.AlertDialog.Builder(this);
                if (State == WaitingState.Format_Ndef)
                {
                    msg.SetTitle("Failed To Format Tag!!!");
                    msg.SetMessage("Tag You Tapped Dosen't Supports 'NdefFormatable' Technology...");
                    msg.SetPositiveButton("OK", (s, a) => { });
                    msg.SetCancelable(true);
                    msg.Create().Show();
                    TxtState.Text = "Failed To Format";
                }
                else if (State == WaitingState.Write_Mifare)
                {
                    msg.SetTitle("Failed To Write Tag!!!");
                    msg.SetMessage("Tag You Tapped Dosen't Supports 'MifareClassic' Technology...");
                    msg.SetPositiveButton("OK", (s, a) => { });
                    msg.SetCancelable(true);
                    msg.Create().Show();
                    TxtState.Text = "Failed To Write";
                }
                else if (State == WaitingState.Write_Ndef)
                {
                    msg.SetTitle("Failed To Write Tag!!!");
                    msg.SetMessage("Tag You Tapped Dosen't Supports 'Ndef' Technology...");
                    msg.SetPositiveButton("OK", (s, a) => { });
                    msg.SetCancelable(true);
                    msg.Create().Show();
                    TxtState.Text = "Failed To Write";
                }
            }
            else TxtMsg.Text = "";
            State = WaitingState.None;
            Terminal.WriteLine("------------------------------------------------------------");
        }
        //void OnReadNdefMessage(NdefMessage message)
        //{
        //    NdefRecord[] array = message.GetRecords();
        //    for (int i = 0; i < array.Length; i++)
        //    {
        //        NdefRecord item = array[i];
        //        Terminal.WriteLine($"Reading Msg [{i}] >> " + Encoding.UTF8.GetString(item.GetPayload()));
        //    }
        //}
        //void OnWriteNdefMessage(NdefMessage message)
        //{
        //    NdefRecord[] array = message.GetRecords();
        //    for (int i = 0; i < array.Length; i++)
        //    {
        //        NdefRecord item = array[i];
        //        Terminal.WriteLine($"Writing Msg [{i}] >> " + Encoding.UTF8.GetString(item.GetPayload()));
        //    }
        //}
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        enum WaitingState
        {
            None,
            Write_Ndef,
            Write_Mifare,
            Format_Ndef,
        }
    }
    public static class Terminal
    {
        private static EditText _cli;
        public static void Init(EditText Cli)
        {
            _cli = Cli;
        }
        public static void WriteLine(string s)
        {
            if (_cli == null) return;
            _cli.Text += "\n" + s;
        }
        public static void Write(string s)
        {
            if (_cli == null) return;
            _cli.Text += s;
        }
        public static void Clear()
        {
            if (_cli == null) return;
            _cli.Text = "";
        }
    }
}


//void resolveIntent(Intent intent)
//{
//    // 1) Parse the intent and get the action that triggered this intent
//    String action = intent.Action;
//    // 2) Check if it was triggered by a tag discovered interruption.
//    if (NfcAdapter.ActionTagDiscovered.Equals(action))
//    {
//        //  3) Get an instance of the TAG from the NfcAdapter
//        Tag tagFromIntent = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
//        // 4) Get an instance of the Mifare classic card from this TAG intent
//        MifareClassic mfc = MifareClassic.Get(tagFromIntent);

//        byte[] data;

//        try
//        {       //  5.1) Connect to card
//            mfc.Connect();
//            bool auth = false;
//            // 5.2) and get the number of sectors this card has..and loop thru these sectors
//            int secCount = mfc.SectorCount;
//            int bCount = 0;
//            int bIndex = 0;
//            //for (int j = 0; j < secCount; j++)
//            //{
//            // 6.1) authenticate the sector
//            auth = mfc.AuthenticateSectorWithKeyA(1, MifareClassic.KeyNfcForum.ToArray());
//            if (!auth) auth = mfc.AuthenticateSectorWithKeyA(1, MifareClassic.KeyDefault.ToArray());
//            if (auth)
//            {
//                // 6.2) In each sector - get the block count
//                bCount = mfc.GetBlockCountInSector(1);
//                bIndex = 0;
//                for (int i = 0; i < bCount; i++)
//                {
//                    bIndex = mfc.SectorToBlock(1);
//                    // 6.3) Read the block
//                    data = mfc.ReadBlock(bIndex);
//                    // 7) Convert the data into a string from Hex format.               
//                    Terminal.WriteLine(Encoding.ASCII.GetString(data));
//                    bIndex++;
//                }
//            }
//            else
//            { // Authentication failed - Handle it

//            }
//            //}
//            mfc.Close();
//        }
//        catch (IOException e)
//        {

//        }
//    }
//}

//private void WriteCard(Intent intent)
//{
//    Terminal.WriteLine("wri");
//    String action = intent.Action;
//    if (NfcAdapter.ActionTagDiscovered.Equals(action))
//    {
//        Tag tagFromIntent = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
//        MifareClassic mfc = MifareClassic.Get(tagFromIntent);
//        try
//        {
//            mfc.Connect();
//            bool authA = mfc.AuthenticateSectorWithKeyA(1, MifareClassic.KeyNfcForum.ToArray());
//            if (!authA) authA = mfc.AuthenticateSectorWithKeyA(1, MifareClassic.KeyDefault.ToArray());
//            Terminal.WriteLine(authA.ToString());
//            mfc.WriteBlock(mfc.SectorToBlock(1), new char[] { 'A', 'l', 'v', 'a', 'r', 'e', 'z', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ', ' ' }.Select(s => (byte)s).ToArray());
//            mfc.Close();
//        }
//        catch (IOException ioe)
//        {
//            Terminal.WriteLine(ioe.ToString());
//        }
//        catch (Exception e)
//        {
//            Terminal.WriteLine(e.ToString());
//        }
//    }
//    return;
//}

//public bool writeTag(NdefMessage message, Tag tag)
//{
//    int size = message.ToByteArray().Length;
//    try
//    {
//        Ndef ndef = Ndef.Get(tag);
//        if (ndef != null)
//        {
//            ndef.Connect();

//            foreach (var item in ndef.NdefMessage.GetRecords())
//            {
//                Terminal.WriteLine("MSG >> " + Encoding.UTF8.GetString(item.GetPayload()));
//            }
//            if (!ndef.IsWritable)
//            {
//                Toast.MakeText(this,
//                "Error: tag not writeable",
//                ToastLength.Long).Show();
//                return false;
//            }
//            if (ndef.MaxSize < size)
//            {
//                Toast.MakeText(this,
//                "Error: tag too small",
//                ToastLength.Long).Show();
//                return false;
//            }
//            ndef.WriteNdefMessage(message);
//            return true;
//        }
//        else
//        {
//            NdefFormatable format = NdefFormatable.Get(tag);
//            if (format != null)
//            {
//                try
//                {
//                    format.Connect();
//                    format.Format(message);
//                    return true;
//                }
//                catch (IOException e)
//                {
//                    return false;
//                }
//            }
//            else
//            {
//                return false;
//            }
//        }
//    }
//    catch (Exception e)
//    {
//        return false;
//    }
//}