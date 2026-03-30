using Nec.Web.Interfaces;
using Nec.Web.Models;
using NPOI.HPSF;

namespace Nec.Web.Services
{
    public interface IMyserviceSingleton
    {
        string GetMessage();
        int GetCount();
    }
    public interface IMyserviceAddScope
    {
        string GetMessage();
        int GetCount();
    }
    public interface IMyserviceTransient
    {
        string GetMessage();
        int GetCount();
    }
    public class Test :IMyserviceSingleton, IMyserviceTransient, IMyserviceAddScope
    {
        public int _count;
        public readonly string _message;
        public string Guidd { get; set; }
        public Test() {

            _count = 0;
            _message = $"service created at {DateTime.Now}";
            this.Guidd = Guid.NewGuid().ToString();
        }

        public int GetCount()
        {
            return _count++;
        }

        public string GetMessage()
        {
            return _message;
        }
    }
}
