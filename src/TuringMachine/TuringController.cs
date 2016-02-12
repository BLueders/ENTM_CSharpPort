using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpNeat.Phenomes;

namespace ENTM.TuringMachine
{
    class TuringController : IController
    {
        private ITuringMachine _turingMachine;
        private IBlackBox _blackBox;

        public ITuringMachine TuringMachine => _turingMachine;

        public TuringController(IBlackBox blackBox, TuringMachine turingMachine)
        {
            _turingMachine = turingMachine;
            _blackBox = blackBox;
            }

        public double[] Run(double[] inputs)
        {
            for (int i = 0; i < UPPER; i++)
            {
                
            }
        }
    }
}
