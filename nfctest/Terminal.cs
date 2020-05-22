using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Internal;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;

namespace nfctest
{
    public class CLIView
    {
        public int ID;
        readonly Activity activity;

        public readonly CoordinatorLayout Parent;
        readonly RelativeLayout relativeLayout;
        readonly ScrollView scrollView;

        readonly FlowLayout promptPanel;
        readonly EditText promptQ;
        readonly EditText promptA;

        readonly EditText console;
        readonly TextView Status;
        readonly FloatingActionButton FAB;

        public TerminalUI CLI;
        readonly Timer HeartBeat;

        public static Android.Graphics.Color DefaultBackColor = new Android.Graphics.Color(21, 21, 21);

        public CLIView(int id, CoordinatorLayout parentView, Activity ThisActivity, Android.Graphics.Color BackColor)
        {
            ID = id;
            activity = ThisActivity;

            Parent = new CoordinatorLayout(activity);
            parentView.AddView(Parent);

            Parent.SetBackgroundColor(BackColor);

            relativeLayout = new RelativeLayout(activity);
            relativeLayout.SetBackgroundColor(BackColor);

            Parent.AddView(relativeLayout);


            scrollView = new ScrollView(activity);
            scrollView.SetBackgroundColor(BackColor);

            relativeLayout.AddView(scrollView, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));


            console = new EditText(activity)
            {
                InputType = Android.Text.InputTypes.Null,
                Focusable = false,
                OverScrollMode = OverScrollMode.Always,
                ScrollBarStyle = ScrollbarStyles.InsideInset,
                VerticalScrollBarEnabled = true
            };
            console.SetTextIsSelectable(false);
            console.SetSingleLine(false);
            console.SetBackgroundColor(BackColor);
            console.SetTextColor(Android.Graphics.Color.White);

            console.Text = " Starting ConsoleDroid... ";

            scrollView.AddView(console, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));



            Status = new TextView(activity)
            {
                TextAlignment = TextAlignment.Center,
                Gravity = GravityFlags.Bottom,
                Text = "status"
            };

            Status.SetBackgroundColor(BackColor);
            Status.SetTextColor(Android.Graphics.Color.Azure);
            relativeLayout.AddView(Status, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            RelativeLayout.LayoutParams statusParams = Status.LayoutParameters as RelativeLayout.LayoutParams;
            statusParams.AddRule(LayoutRules.AlignParentBottom);


            FAB = new FloatingActionButton(activity)
            {

            };

            Parent.AddView(FAB);
            CoordinatorLayout.LayoutParams FABLayout = FAB.LayoutParameters as CoordinatorLayout.LayoutParams;
            FABLayout.Width = -2; FABLayout.Height = -2;
            FABLayout.SetMargins(160, 160, 80, 80);
            FABLayout.Gravity = (int)(GravityFlags.Bottom | GravityFlags.End);
            FAB.SetImageResource(Resource.Drawable.round_pause_circle_filled_24);
            FAB.Click += FabOnClick;



            promptPanel = new FlowLayout(activity);
            relativeLayout.AddView(promptPanel, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            RelativeLayout.LayoutParams promptPanelLayout = promptPanel.LayoutParameters as RelativeLayout.LayoutParams;
            promptPanelLayout.AddRule(LayoutRules.AlignParentBottom);

            promptQ = new EditText(activity)
            {
                InputType = Android.Text.InputTypes.Null,
                Focusable = true
            };
            promptQ.SetTextIsSelectable(true);
            promptQ.SetSingleLine(false);
            promptQ.SetBackgroundColor(BackColor);
            promptQ.SetTextColor(Android.Graphics.Color.LightYellow);
            promptQ.Text = "How old are you?";

            promptPanel.AddView(promptQ, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            promptA = new EditText(activity)
            {
                InputType = Android.Text.InputTypes.ClassText,
                Focusable = true
            };
            promptA.SetBackgroundColor(BackColor);
            promptA.SetTextColor(Android.Graphics.Color.LightYellow);
            promptA.SetImeActionLabel("enter", Android.Views.InputMethods.ImeAction.Send);
            promptA.EditorAction += PromptA_EditorAction;

            promptPanel.AddView(promptA, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.WrapContent));

            promptPanel.Visibility = ViewStates.Invisible;

            CLI = new TerminalUI(SetConsoleText, SetStatus, Prompt);

            HeartBeat = new Timer(500);
            HeartBeat.Elapsed += HeartBeat_Elapsed;
            HeartBeat.Start();
        }


        public void SetConsoleText(string s)
        {
            activity.RunOnUiThread(() =>
            {

                console.Text = s;
                console.RefreshDrawableState();
                //scrollView.ScrollTo(0, (int)((console.LineHeight + console.LineSpacingExtra) * (console.LineCount + 1)));
                scrollView.FullScroll(FocusSearchDirection.Down);

            });
        }

        public void SetStatus(string s)
        {
            activity.RunOnUiThread(() =>
            {
                Status.Text = s;
            });
        }






        bool IsPrompting = false;
        bool PromptAChanged = false;
        readonly Array PromptLock = Array.Empty<int>();

        public string Prompt(string q)
        {
            while (IsPrompting)
            {
                System.Threading.Thread.Sleep(100);
            }

            lock (PromptLock)
            {
                IsPrompting = true;
                PromptAChanged = false;

                activity.RunOnUiThread(() =>
                {
                    promptPanel.Visibility = ViewStates.Visible;
                    promptQ.Text = q;
                    promptA.Text = "";

                    scrollView.LayoutParameters.Height = relativeLayout.Height - promptPanel.Height;
                    promptA.RequestFocus();
                });



                while (!PromptAChanged)
                {
                    System.Threading.Thread.Sleep(100);

                    activity.RunOnUiThread(() =>
                    {
                        scrollView.LayoutParameters.Height = relativeLayout.Height - promptPanel.Height;
                    });
                }
                string res = promptA.Text;
                PromptAChanged = false;

                activity.RunOnUiThread(() =>
                {
                    scrollView.LayoutParameters.Height = -2;
                    promptQ.Text = "";
                    promptPanel.Visibility = ViewStates.Invisible;
                });


                IsPrompting = false;
                return res;
            }
        }


        private void PromptA_EditorAction(object sender, TextView.EditorActionEventArgs e)
        {
            PromptAChanged = true;
        }

        private void FabOnClick(object sender, EventArgs eventArgs)
        {
            // View view = (View)sender;
            // Snackbar.Make(view, "Replace with your own action", Snackbar.LengthLong)
            //     .SetAction("Action", (Android.Views.View.IOnClickListener)null).Show();


            if (CLI.ToggleHold())
            {
                console.Focusable = true;
                console.SetTextIsSelectable(true);
                FAB.SetImageResource(Resource.Drawable.round_play_circle_filled_24);
            }
            else
            {
                console.SetTextIsSelectable(false);
                console.Focusable = false;
                FAB.SetImageResource(Resource.Drawable.round_pause_circle_filled_24);
            }
        }


        uint HeartBeats = 0;
        private void HeartBeat_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            HeartBeats++;

            CLI.SetStatus($"{GC.GetTotalMemory(false) / 1048576} MB ");

            if (!CLI.IsOnHold)
            {
                activity.RunOnUiThread(() =>
                {
                    scrollView.FullScroll(FocusSearchDirection.Down);

                });
            }


            if (HeartBeats % 8 == 0)
            {

            }

        }


    }
}