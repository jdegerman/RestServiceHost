using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestService
{
    public class ServiceClass
    {
        public List<int> GetList(int count)
        {
            return Enumerable.Range(1, count).ToList();
        }
    }
}
