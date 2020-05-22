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
        private EditText CLI;
        private MifareNFC NFC;
        int block = 0;
        byte[] data;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            CLI = FindViewById<EditText>(Resource.Id.editText1);
            FindViewById<FloatingActionButton>(Resource.Id.floatingActionButton1).Click += MainActivity_Click;

            Teminal.Init(CLI);
            Teminal.WriteLine("Initializeing...");
            Teminal.WriteLine(NFC.Initialize(this).ToString());
            NFC.Enable_WriteDataToBlock_WhenTagDetected();
            NFC.Enable_ReadDataFromBlock_WhenTagDetected();
            NFC.OnNewTagDiscovered += NewTagDetected;
            NFC.OnReadingBlock += OnReadBlock;
            NFC.OnWritingBlock += OnWriteBlock;
        }

        private void MainActivity_Click(object sender, System.EventArgs e)
        {
            data = Encoding.ASCII.GetBytes("qhf");
            Teminal.WriteLine("Set Writing Data >> " + NFC.WriteDataToBlock_WhenTagDetected(block, data).ToString());
        }

        protected override void OnResume()
        {
            base.OnResume();
            NFC.OnResume();
        }
        protected override void OnPause()
        {
            base.OnPause();
            NFC.OnPause();
        }
        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            Teminal.WriteLine("New Intent >> " + NFC.OnNewIntent(intent).ToString());
        }

        void NewTagDetected(MifareNFC.TagInfo? info)
        {

        }
        void OnReadBlock(int block,byte[] data)
        {

        }
        void OnWriteBlock(int block, byte[] data)
        {

        }

        //public void WriteToTag(Intent intent, string content)
        //{
        //    var tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
        //    if (tag != null)
        //    {
        //        Ndef ndef = Ndef.Get(tag);
        //        if (ndef != null && ndef.IsWritable)
        //        {
        //            var payload = Encoding.ASCII.GetBytes(content);
        //            var mimeBytes = Encoding.ASCII.GetBytes("text/plain");
        //            var record = new NdefRecord(NdefRecord.TnfWellKnown, mimeBytes, new byte[0], payload);
        //            var ndefMessage = new NdefMessage(new[] { record });
        //            Teminal.WriteLine("------BEGIN WRITING------");
        //            ndef.Connect();
        //            ndef.WriteNdefMessage(ndefMessage);
        //            ndef.Close();
        //            Teminal.WriteLine("------END WRITING------");
        //        }
        //    }
        //}
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