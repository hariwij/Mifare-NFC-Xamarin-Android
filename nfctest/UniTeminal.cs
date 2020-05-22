using System;

namespace nfctest
{
    public interface IUniCLI
    {
        void Log(string s);
        void LogAppend(string s);
        void LogSpecial(string s);
        void LogError(Exception ex, string Msg = "");
        string Prompt(string s);
        void SetStatus(string s);
        void Clear();
        void Hold();
        void Unhold();
    }
}
