using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;
using Android.Support.Design.Widget;
using Android.Content;
using System.Text;

namespace MifareNFCLib.Demo
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    public class MainActivity : AppCompatActivity
    {
        private EditText CLI;
        private readonly MifareNFC NFC = new MifareNFC();
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

            Terminal.Init(CLI);

            Terminal.WriteLine("Initializeing...");
            Terminal.WriteLine(NFC.Initialize(this).ToString());

            NFC.Enable_WriteDataToBlock_WhenTagDetected();
            NFC.Enable_ReadDataFromBlock_WhenTagDetected();

            NFC.OnNewTagDiscovered += NewTagDetected;
            NFC.OnReadingBlock += OnReadBlock;
            NFC.OnWritingBlock += OnWriteBlock;
        }
        private void MainActivity_Click(object sender, System.EventArgs e)
        {
            data = Encoding.ASCII.GetBytes("qhf");
            Terminal.WriteLine("Set Writing Data >> " + NFC.WriteDataToBlock_WhenTagDetected(block, data).ToString());
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
            Terminal.WriteLine("New Intent >> " + NFC.OnNewIntent(intent).ToString());
        }
        void NewTagDetected(MifareNFC.TagInfo? info)
        {
            Terminal.WriteLine(info.ToString());
        }
        void OnReadBlock(int block,byte[] data)
        {
            string dta = "";
            for (int ii = 0; ii < data.Length; ii++)
            {
                if (!string.IsNullOrEmpty(dta))
                    dta += "-";
                dta += data[ii].ToString("X2");
            }
            Terminal.WriteLine($"Reading >> [Block : {block}] [Data : {dta}]");
        }
        void OnWriteBlock(int block, byte[] data)
        {
            string dta = "";
            for (int ii = 0; ii < data.Length; ii++)
            {
                if (!string.IsNullOrEmpty(dta))
                    dta += "-";
                dta += data[ii].ToString("X2");
            }
            Terminal.WriteLine($"Writing >> [Block : {block}] [Data : {dta}]");
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