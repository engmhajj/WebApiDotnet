using System;
using System.Linq.Expressions;

namespace webapi.Interfaces;

public interface IBackgroundJobWrapper
{
    string Enqueue(Expression<Action> methodCall);
}
