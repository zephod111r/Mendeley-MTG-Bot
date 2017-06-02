using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HipchatMTGBot
{
    internal interface ICommandOption
    {
        string Name { get; set; }

        T Value<T>();

        void Value<T>(T value);
    }
}
