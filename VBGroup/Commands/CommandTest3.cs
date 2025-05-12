using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.Attributes;
using Nice3point.Revit.Toolkit.External;

namespace VBGroup.Commands
{
    [UsedImplicitly]
    [Transaction(TransactionMode.Manual)]
    public class CommandTest3 : ExternalCommand
    {
        public override void Execute()
        {
        }
    }
}
