using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace living_log_cli
{
    public class Activity
    {
        public Timestamp Timestamp;
        public Category Type;
        public IData Info;
    }
    
    public interface IData
    {
        string ToString();
    }

    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; }

        public override string ToString() { return Id.ToString() + " " + Name; }
    }
}
