using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Nfc;
using Android.Support.Design.Widget;
using Android.Content;
using System.Text;
using Android.Nfc.Tech;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

namespace nfctest
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private NfcAdapter _nfcAdapter;
        public object NFCUtil { get; private set; }
        private EditText CLI;

        bool Write = false;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            CLI = FindViewById<EditText>(Resource.Id.editText1);
            FindViewById<FloatingActionButton>(Resource.Id.floatingActionButton1).Click += MainActivity_Click;

            Teminal.Init(CLI);
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
            Teminal.WriteLine("CREATE");
        }

        private void MainActivity_Click(object sender, System.EventArgs e)
        {
            Write = true;
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (_nfcAdapter == null)
            {
                var alert = new Android.App.AlertDialog.Builder(this).Create();
                alert.SetMessage("NFC is not supported on this device.");
                alert.SetTitle("NFC Unavailable");
                alert.Show();
            }
            else
            {
                var tagDetected = new IntentFilter(NfcAdapter.ActionTagDiscovered);
                var ndefDetected = new IntentFilter(NfcAdapter.ActionNdefDiscovered);
                var techDetected = new IntentFilter(NfcAdapter.ActionTechDiscovered);

                var filters = new[] { ndefDetected, tagDetected, techDetected };

                var intent = new Intent(this, this.GetType()).AddFlags(ActivityFlags.SingleTop);

                var pendingIntent = PendingIntent.GetActivity(this, 0, intent, 0);

                _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, filters, null);
            }
        }
        protected override void OnPause()
        {
            base.OnPause();
            if (_nfcAdapter != null)
                _nfcAdapter.DisableForegroundDispatch(this);
        }
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            Teminal.WriteLine("NEW INTENT");

            if (intent.Extras.IsEmpty)
            {
                Teminal.WriteLine(">>> empty");
            }
            else
            {
                Teminal.WriteLine(">>> Not empty");
            }

            if (Write)
            {
                WriteToTag(intent, "4395");
                Write = false;
            }
            if (intent.Action == NfcAdapter.ActionTagDiscovered)
            {
                var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;

                if (tag == null)
                {
                    Teminal.WriteLine("------NULL READING------");
                    return;
                }
                MifareClassic mifc = MifareClassic.Get(tag);
                try
                {
                    Teminal.WriteLine("------BEGIN READING------");
                    mifc.ConnectAsync().Wait();
                    Teminal.WriteLine("SectorCount:  > " + mifc.SectorCount);
                    Teminal.WriteLine("BlockCount in Sector 1 > " + mifc.GetBlockCountInSector(1));
                    byte[] blargh = new byte[6];
                    MifareClassic.KeyDefault.CopyTo(blargh, 0);
                    if (Task.Factory.StartNew<bool>(() => mifc.AuthenticateSectorWithKeyA(1, blargh)).Result)
                    {
                        Teminal.WriteLine("------AUTH A OK------");
                        blargh = new byte[6];
                        MifareClassic.KeyDefault.CopyTo(blargh, 0);
                        if (Task.Factory.StartNew<bool>(() => mifc.AuthenticateSectorWithKeyB(1, blargh)).Result)
                        {
                            Teminal.WriteLine("------AUTH B OK------");
                            for (int s = 0; s < mifc.SectorCount; s++)
                            {
                                Teminal.WriteLine($"------READ SECTION [{s}]------");
                                int firstBlock = mifc.SectorToBlock(s);
                                int lastBlock = firstBlock + 4;
                                List<byte[]> lstBlocks = new List<byte[]>();
                                for (int i = firstBlock; i < lastBlock; i++)
                                {
                                    Teminal.WriteLine("READING BLOCK > " + i);
                                    byte[] block = mifc.ReadBlockAsync(i).Result;
                                    lstBlocks.Add(block);
                                }
                                string BlockData = string.Empty;
                                foreach (var item in lstBlocks)
                                {
                                    BlockData += Encoding.ASCII.GetString(item) + "\r\n";
                                }
                                Teminal.WriteLine("DATA > " + BlockData);
                                Teminal.WriteLine($"------END READ SECTION [{s}]------");
                            }
                        }
                    }
                    mifc.Close();
                }
                catch (Exception ex)
                {
                    Teminal.WriteLine(ex.ToString());
                }

                //if (tag != null)
                //{
                //    // First get all the NdefMessage
                //    var rawMessages = intent.GetParcelableArrayExtra(NfcAdapter.ExtraNdefMessages);
                //    if (rawMessages != null)
                //    {
                //        var msg = (NdefMessage)rawMessages[0];

                //        // Get NdefRecord which contains the actual data
                //        var record = msg.GetRecords()[0];
                //        if (record != null)
                //        {
                //            if (record.Tnf == NdefRecord.TnfWellKnown) // The data is defined by the Record Type Definition (RTD) specification available from http://members.nfc-forum.org/specs/spec_list/
                //            {
                //                // Get the transfered data
                //                var data = Encoding.ASCII.GetString(record.GetPayload());
                //                Teminal.WriteLine("Data >>>> " + data);
                //            }
                //        }
                //    }
                //}
            }
            Teminal.WriteLine("------END READING------");
        }

        public void WriteToTag(Intent intent, string content)
        {
            var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            if (tag != null)
            {
                Ndef ndef = Ndef.Get(tag);
                if (ndef != null && ndef.IsWritable)
                {
                    var payload = Encoding.ASCII.GetBytes(content);
                    var mimeBytes = Encoding.ASCII.GetBytes("text/plain");
                    var record = new NdefRecord(NdefRecord.TnfWellKnown, mimeBytes, new byte[0], payload);
                    var ndefMessage = new NdefMessage(new[] { record });
                    Teminal.WriteLine("------BEGIN WRITING------");
                    ndef.Connect();
                    ndef.WriteNdefMessage(ndefMessage);
                    ndef.Close();
                    Teminal.WriteLine("------END WRITING------");
                }
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
    }
    public static class Teminal
    {
        private static EditText _cli;
        public static void Init(EditText Cli)
        {
            _cli = Cli;
        }
        public static void WriteLine(string s)
        {
            _cli.Text += "\n" + s;
        }
        public static void Write(string s)
        {
            _cli.Text += s;
        }
        public static void Clear()
        {
            _cli.Text = "";
        }
    }
}