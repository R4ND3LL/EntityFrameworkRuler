using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using NorthwindModel.Context;
using NorthwindModel.Models;

[assembly: DesignTimeServicesReference("EntityFrameworkRuler.Design.RuledDesignTimeServices, EntityFrameworkRuler.Design")]
// ReSharper disable CollectionNeverUpdated.Local
// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable PossibleNullReferenceException

namespace NorthwindModel;

internal class Program {
    private static void Main() {
        /***************************************************************************************/
        /********* Do not run this project.  It is a target for scaffolding only. *********/
        /***************************************************************************************/

        Console.WriteLine("This is a fake test project to illustrate rule application only!");
    }
}

