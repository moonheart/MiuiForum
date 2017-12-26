using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MiuiForum
{
    class Program
    {
        static void Main(string[] args)
        {
            new MiuiForumAutoReply().Start();
        }
    }
}
