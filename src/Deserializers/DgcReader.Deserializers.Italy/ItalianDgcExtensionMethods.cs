using DgcReader.Deserializers.Italy.Models;
using GreenpassReader.Models;

namespace DgcReader
{
    /// <summary>
    /// Extension methods for <see cref="ItalianDGC"/>
    /// </summary>
    public static class ItalianDgcExtensionMethods
    {
        /// <summary>
        /// Try to cast the EuDGC as the ItalianDGG customization
        /// </summary>
        /// <param name="dgc"></param>
        /// <returns></returns>
        public static ItalianDGC? AsItalianDgc(this EuDGC dgc) => dgc as ItalianDGC;
    }
}
