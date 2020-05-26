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

namespace MifareNFCLib.Demo
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private EditText CLI;
        private readonly MifareNFC NFC = new MifareNFC();
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);
            CLI = FindViewById<EditText>(Resource.Id.editText1);
            FindViewById<FloatingActionButton>(Resource.Id.floatingActionButton1).Click += MainActivity_Click;

            Terminal.Init(CLI);

            Terminal.WriteLine("Initializeing...");
            Terminal.WriteLine(NFC.Initialize(this).ToString());
            NFC.Initialize(this);
            NFC.Enable_WriteDataToBlock_WhenTagDetected();
            NFC.OnNewTagDiscovered += NewTagDetected;
            NFC.OnReadingMessage += OnReadBlock;
            NFC.OnWritingMessage += OnWriteBlock;
        }
        private void MainActivity_Click(object sender, System.EventArgs e)
        {
            NdefRecord record = NdefRecord.CreateMime("text/plain", Encoding.ASCII.GetBytes("Harindu Chinthaka Wijesinghe"));
            NdefRecord record1 = NdefRecord.CreateMime("text/plain", Encoding.ASCII.GetBytes("Hasindu"));
            //NdefRecord record = NdefRecord.CreateTextRecord("", "Harindu");
            //NdefRecord record1 = NdefRecord.CreateTextRecord("", "Hasindu");
            NdefMessage message = new NdefMessage(new NdefRecord[] { record, record1 });
            Terminal.WriteLine("Set Writing Data >> " + NFC.WriteDataToBlock_WhenTagDetected(message).ToString());
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
            //Tag tag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;

            //if (mWriteMode && NfcAdapter.ActionTagDiscovered.Equals(intent.Action))
            //{
            //    Tag detectedTag = intent.GetParcelableExtra(NfcAdapter.ExtraTag) as Tag;
            //    NdefRecord record = NdefRecord.CreateMime("text/plain", Encoding.ASCII.GetBytes("Harindu Chinthaka Wijesinghe"));
            //    NdefRecord record1 = NdefRecord.CreateMime("text/plain", Encoding.ASCII.GetBytes("Hasindu"));
            //    //NdefRecord record = NdefRecord.CreateTextRecord("", "Harindu");
            //    //NdefRecord record1 = NdefRecord.CreateTextRecord("", "Hasindu");
            //    NdefMessage message = new NdefMessage(new NdefRecord[] { record,record1 });
            //    if (writeTag(message, detectedTag))
            //    {
            //        Toast.MakeText(this, "Success: Wrote placeid to nfc tag", ToastLength.Long).Show();
            //    }
            //}
        }
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
        void NewTagDetected(MifareNFC.TagInfo? info)
        {
            Terminal.WriteLine(info.ToString());
        }
        void OnReadBlock(NdefMessage message)
        {
            NdefRecord[] array = message.GetRecords();
            for (int i = 0; i < array.Length; i++)
            {
                NdefRecord item = array[i];
                Terminal.WriteLine($"Reading Msg [{i}] >> " + Encoding.UTF8.GetString(item.GetPayload()));
            }
        }
        void OnWriteBlock(NdefMessage message)
        {
            NdefRecord[] array = message.GetRecords();
            for (int i = 0; i < array.Length; i++)
            {
                NdefRecord item = array[i];
                Terminal.WriteLine($"Writing Msg [{i}] >> " + Encoding.UTF8.GetString(item.GetPayload()));
            }
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
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