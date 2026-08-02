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
            var list = source as List<T> ?? source.ToList();
            var queryable = list.AsQueryable();
            var asyncQueryable = list.AsAsyncQueryable();
            var mockDbSet = new Mock<DbSet<T>>();

            mockDbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(asyncQueryable.Provider);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(() => list.AsQueryable().Expression);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(() => list.AsQueryable().ElementType);
            mockDbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => list.GetEnumerator());
            mockDbSet.As<IAsyncEnumerable<T>>()
                .Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
                .Returns(() => new TestAsyncEnumerator<T>(list.GetEnumerator()));

            mockDbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(entity => list.Add(entity));
            mockDbSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
                .Callback<T, CancellationToken>((entity, token) => list.Add(entity))
                .ReturnsAsync((T entity, CancellationToken token) => null!);

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
            var cleanExpression = IncludeVisitor.StripIncludes(expression);
            return new TestAsyncEnumerable<TEntity>(cleanExpression);
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            var cleanExpression = IncludeVisitor.StripIncludes(expression);
            return new TestAsyncEnumerable<TElement>(cleanExpression);
        }

        public object? Execute(Expression expression)
        {
            var cleanExpression = IncludeVisitor.StripIncludes(expression);
            var eq = new EnumerableQuery<TEntity>(cleanExpression);
            return ((IQueryProvider)eq).Execute(cleanExpression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var cleanExpression = IncludeVisitor.StripIncludes(expression);
            var eq = new EnumerableQuery<TEntity>(cleanExpression);
            return ((IQueryProvider)eq).Execute<TResult>(cleanExpression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var cleanExpression = IncludeVisitor.StripIncludes(expression);
            var typeofTResult = typeof(TResult);
            if (typeofTResult.IsGenericType && typeofTResult.GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = typeofTResult.GetGenericArguments()[0];
                var eq = new EnumerableQuery<TEntity>(cleanExpression);
                var executionResult = ((IQueryProvider)eq).Execute(cleanExpression);
                return (TResult)typeof(Task).GetMethod("FromResult")!
                    .MakeGenericMethod(resultType)
                    .Invoke(null, new[] { executionResult })!;
            }

            var eq2 = new EnumerableQuery<TEntity>(cleanExpression);
            var res = ((IQueryProvider)eq2).Execute(cleanExpression);
            return (TResult)typeof(Task).GetMethod("FromResult")!
                .MakeGenericMethod(typeofTResult)
                .Invoke(null, new object?[] { res })!;
        }
    }

    internal class IncludeVisitor : ExpressionVisitor
    {
        public static Expression StripIncludes(Expression expression)
        {
            return new IncludeVisitor().Visit(expression);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.Name is "Include" or "ThenInclude")
            {
                return Visit(node.Arguments[0]);
            }
            return base.VisitMethodCall(node);
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
