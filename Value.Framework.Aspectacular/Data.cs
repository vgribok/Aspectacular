using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Value.Framework.Aspectacular
{
    /// <summary>
    /// Interface of the non-select database storage command execution.
    /// </summary>
    /// <typeparam name="TCmd"></typeparam>
    public interface IStorageCommandRunner<TCmd>
        where TCmd : class, IDisposable, new()
    {
        /// <summary>
        /// Command that returns no value except for int returned by underlying DB engine.
        /// </summary>
        /// <param name="callExpression"></param>
        /// <returns>Value returned by DB engines like SQL server when insert and update statements are run that return no value</returns>
        int ExecuteCommand(Expression<Func<TCmd>> callExpression);

        /// <summary>
        /// Command that returns scalar value.
        /// </summary>
        /// <typeparam name="TOut"></typeparam>
        /// <param name="callExpression"></param>
        /// <returns></returns>
        TOut ExecuteCommand<TOut>(Expression<Func<TCmd, TOut>> callExpression);
    }
}
