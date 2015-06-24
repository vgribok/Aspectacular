using System;
using System.Threading;

namespace Aspectacular
{
    public class Lazier<T> : Lazy<T>
    {
        /// <summary>
        /// Initializes a new instance of the class. When lazy initialization occurs, the default constructor of the target type is used.
        /// </summary>
        public Lazier() : base()
        {
            
        }
        
        /// <summary>
        /// Initializes a new instance of  class. When lazy initialization
        /// occurs, the default constructor of the target type and the specified initialization
        /// mode are used.
        /// </summary>
        /// <param name="isThreadSafe">true to make this instance usable concurrently by multiple threads; false to make the instance usable by only one thread at a time.</param>
        public Lazier(bool isThreadSafe) : base(isThreadSafe)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the System.Lazier<T> class. When lazy initialization
        /// occurs, the specified initialization function is used.
        /// </summary>
        /// <param name="valueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        /// <exception cref="ArgumentNullException">valueFactory is null.</exception>
        public Lazier(Func<T> valueFactory)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the class that uses the default
        /// constructor of T and the specified thread-safety mode.
        /// </summary>
        /// <param name="mode">One of the enumeration values that specifies the thread safety mode.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">mode contains an invalid value.</exception>
        public Lazier(LazyThreadSafetyMode mode) : base(mode)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the class. When lazy initialization
        /// occurs, the specified initialization function and initialization mode are
        /// used.
        /// </summary>
        /// <param name="valueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        /// <param name="isThreadSafe">true to make this instance usable concurrently by multiple threads; false to make this instance usable by only one thread at a time.</param>
        /// <exception cref="System.ArgumentNullException">valueFactory is null.</exception>
        public Lazier(Func<T> valueFactory, bool isThreadSafe)
            : base(valueFactory, isThreadSafe)
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the class that uses the specified
        /// initialization function and thread-safety mode.
        /// </summary>
        /// <param name="valueFactory">The delegate that is invoked to produce the lazily initialized value when it is needed.</param>
        /// <param name="mode">One of the enumeration values that specifies the thread safety mode.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">mode contains an invalid value.</exception>
        /// <exception cref="System.ArgumentNullException">valueFactory is null.</exception>
        public Lazier(Func<T> valueFactory, LazyThreadSafetyMode mode)
            : base(valueFactory, mode)
        {
            
        }

        /// <summary>
        /// Implicitly coverts Lazy to value.
        /// </summary>
        /// <param name="lazy"></param>
        /// <returns></returns>
        public static implicit operator T(Lazier<T> lazy)
        {
            return lazy.Value;
        }
    }
}
