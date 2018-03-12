using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    public class ObjectHeart
    {
        private Timer heartbeat;
        private DateTime m_LastBeat = DateTime.Now;

        public ObjectHeart()
        {
            heartbeat = new Timer(CheckThreadsDelegate, this, 0, 45000);
        }

        private void CheckThreads()
        {
            DateTime now = DateTime.Now;
            DateTime then = now.AddMinutes(-5);
            if (then > m_LastBeat)
            {
                StartHeart();
            }
        }

        protected virtual void StartHeart() { Console.Write("Keep Clear! Heartbeat failure for: " + this.ToString() + "\nRestarting Heart! Clear!"); }

        static private void CheckThreadsDelegate(Object o)
        {
            ObjectHeart that = (ObjectHeart)o;
            that.CheckThreads();
        }

        public void Beat()
        {
            m_LastBeat = DateTime.Now;
        }
    }
}
