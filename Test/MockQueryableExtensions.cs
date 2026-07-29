using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    public static class MockQueryableExtensions
    {
        public static IQueryable<T> AsAsyncQueryable<T>(this IEnumerable<T> source)
        {
            return new TestAsyncEnumerable<T>(source);
        }

        public static Mock<DbSet<T>> AsMockDbSet<T>(this IEnumerable<T> source) where T : class
        {
            var queryable = source.AsQueryable();
            var asyncQueryable = source.AsAsyncQueryable();
            var mockDbSet = new Mock<DbSet<T>>();

            mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(asyncQueryable.Provider);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
            mockDbSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

            return mockDbSet;
        }
    }

    public class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        public TestAsyncQueryProvider(IQueryProvider inner)
        {
        }

        public IQueryable CreateQuery(Expression expression)
        {
            return new TestAsyncEnumerable<TEntity>(expression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object? Execute(Expression expression)
        {
            var eq = new EnumerableQuery<TEntity>(expression);
            return ((IQueryProvider)eq).Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var eq = new EnumerableQuery<TEntity>(expression);
            return ((IQueryProvider)eq).Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var typeofTResult = typeof(TResult);
            if (typeofTResult.IsGenericType && typeofTResult.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = typeofTResult.GetGenericArguments()[0];
                var eq = new EnumerableQuery<TEntity>(expression);
                var executionResult = ((IQueryProvider)eq).Execute(expression);
                return (TResult)typeof(Task).GetMethod("FromResult")!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new[] { executionResult })!;
            }

            var eq2 = new EnumerableQuery<TEntity>(expression);
            var res = ((IQueryProvider)eq2).Execute(expression);
            return (TResult)typeof(Task).GetMethod("FromResult")!
                .MakeGenericMethod(typeofTResult)
                .Invoke(null, new object?[] { res })!;
        }
    }

    public class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable)
        { }

        public TestAsyncEnumerable(Expression expression) : base(expression)
        { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    public class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }

        public T Current => _inner.Current;
    }
}
