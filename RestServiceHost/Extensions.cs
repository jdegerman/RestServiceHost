using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RestServiceHost
{
    public static class Extensions
    {
        public static MethodInfo GetMethodByName(this object o, string name)
        {
            return o.GetType().GetMethod(name);
        }
    }
}
