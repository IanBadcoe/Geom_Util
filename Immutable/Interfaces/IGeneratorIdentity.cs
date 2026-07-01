using System.Collections.Generic;

namespace Geom_Util.Immutable.Interfaces;

public interface IGeneratorIdentity
{
    // just here to hint what the values of SetForbidSpecificMerge
}

public interface IHasGeneratiorIdentities
{
    HashSet<IGeneratorIdentity> GIs { get; }
}
