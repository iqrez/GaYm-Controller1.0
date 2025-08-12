#if WINDOWS
using System.ServiceProcess;
namespace GaymController.Broker {
    public class Program {
        public static void Main() { ServiceBase.Run(new ServiceBase[]{ new GcService() }); }
    }
}
#else
namespace GaymController.Broker {
    public class Program {
        public static void Main() { }
    }
}
#endif
