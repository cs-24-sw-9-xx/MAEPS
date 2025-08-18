using System.Runtime.CompilerServices;

using ZLinq;

[assembly: InternalsVisibleTo("EditModeTests"), InternalsVisibleTo("PlayModeTests")]
[assembly: ZLinqDropIn("Maes", DropInGenerateTypes.Everything)]