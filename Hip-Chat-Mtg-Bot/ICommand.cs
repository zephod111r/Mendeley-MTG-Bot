using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    internal interface ICommand
    {
        string Name { get; set; }

        string RegexPattern { get; set; }

        void AddOption(ICommandOption option, bool required);

        List<ICommandOption> PossibleOptions { get; set; }

        List<ICommandOption> RequiredOptions { get; set; }
    }
}
