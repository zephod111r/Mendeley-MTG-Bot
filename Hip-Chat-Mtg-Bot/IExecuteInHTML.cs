using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    internal interface IExecuteInHTML : ICommand
    {
        string Execute();
    }
}
