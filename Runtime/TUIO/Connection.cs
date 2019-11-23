using System.Net.Sockets;
using System.Threading.Tasks;
using System.Linq;
using OSC.NET;
using System.Net;
using System.Collections.Generic;

namespace TUIO
{
    public class TuioState
    {
        public Dictionary<int, TuioObject> Objects { get; set; }
    }

    public class CompareTuioObject : IEqualityComparer<TuioObject>
    {
        public bool Equals(TuioObject x, TuioObject y) => x.SymbolID == y.SymbolID;
        public int GetHashCode(TuioObject obj) => obj.GetHashCode();
    }

    public class Connection
    {
        private UdpClient _listener;
        private TuioClient _tuioClient = new TuioClient();

        public Connection(int port)
        {
            _listener = new UdpClient(port);
        }

        public async Task<TuioState> Listen()
        {
            do
            {
                var res = await _listener.ReceiveAsync();

                if (res.Buffer == null || res.Buffer.Length == 0)
                    continue;

                var packet = OSCPacket.Unpack(res.Buffer);
                if (packet != null)
                {
                    if (packet.IsBundle())
                    {
                        packet.Values.ForEach(x => _tuioClient.ProcessMessage((OSCMessage)x));
                    }
                    else
                    {
                        _tuioClient.ProcessMessage((OSCMessage)packet);
                    }
                    var tuioObjs = _tuioClient.getTuioObjects();

                    return new TuioState {
                        Objects = tuioObjs
                            .Distinct(new CompareTuioObject())
                            .Where(x => x.TuioState != TuioContainer.TUIO_REMOVED)
                            .ToDictionary(x => x.SymbolID, x => x)
                    };
                }

            } while (true);
        }

        public void Close() => _listener.Close();
    }
}
