using GetReservation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Aeronology.Sabre.Test")]
namespace SabreWebtopTicketingService.Models
{
    public class SabreArunkSector
    {
        private readonly Arunk arunk;
        short seqNo;
        public SabreArunkSector(short seqNo, Arunk arunksec)
        {
            arunk = arunksec;
            this.seqNo = seqNo;

        }

        public string ID => arunk.id;
        public string RPH => arunk.RPH;
        public string LineNumber => arunk.Line.Number;
        public string LineStatus => arunk.Line.Status;
        public short SequenceNo => seqNo;
        public bool Ticketed => false;
    }
}
