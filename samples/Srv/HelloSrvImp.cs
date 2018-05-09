using System.Threading.Tasks;
using FM.Demo;
using Grpc.Core;

namespace Srv
{
    class HelloSrvImp : FM.Demo.HelloSrv.HelloSrvBase
    {
        public override async Task<HiResponse> Hi(HiRequest request, ServerCallContext context)
        {
            //mock api delay 
            await Task.Delay(10000).ConfigureAwait(false);
            return new HiResponse { };
        }
    }
}
