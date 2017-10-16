using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ma.EntityFramework.GraphManager
{
    /// <summary>
    /// Extended DbFunctions.
    /// </summary>
    public static class ExtendedDbFunctions
    {
        /// <summary>
        /// Create lambda expression to check equality of property of enum type with static value.
        /// </summary>
        /// <remarks>
        /// The main purpose of this method is to get rid of auto-generated casting when comparing enums.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// When propertyExpression is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When propertyExpression is not MemberExpression.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity model.</typeparam>
        /// <typeparam name="TEnum">Type of enum.</typeparam>
        /// <param name="propertyExpression">Property expression to select member to check equality.</param>
        /// <param name="value">Value of enum to compare with to check equality.</param>
        /// <returns>Lambda expression to check equality of property of enum type.</returns>
        public static Expression<Func<TEntity, bool>> EnumEquals<TEntity, TEnum>(
            Expression<Func<TEntity, TEnum>> propertyExpression,
            TEnum value)
            where TEntity : class
            where TEnum : struct
        {
            return EnumEquals(propertyExpression, value, false);
        }


        /// <summary>
        /// Create lambda expression to check non-equality of property of enum type with static value.
        /// </summary>
        /// <remarks>
        /// The main purpose of this method is to get rid of auto-generated casting when comparing enums.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// When propertyExpression is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When propertyExpression is not MemberExpression.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity model.</typeparam>
        /// <typeparam name="TEnum">Type of enum.</typeparam>
        /// <param name="propertyExpression">Property expression to select member to check not equality.</param>
        /// <param name="value">Value of enum to compare with to check non-equality.</param>
        /// <returns>Lambda expression to check non-equality of property of enum type.</returns>
        public static Expression<Func<TEntity, bool>> EnumNotEquals<TEntity, TEnum>(
            Expression<Func<TEntity, TEnum>> propertyExpression,
            TEnum value)
            where TEntity : class
            where TEnum : struct
        {
            return EnumEquals(propertyExpression, value, true);
        }


        /// <summary>
        /// Create lambda expression to check non-equality of property of enum type with static value.
        /// </summary>
        /// <remarks>
        /// The main purpose of this method is to get rid of auto-generated casting when comparing enums.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// When propertyExpression is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// When propertyExpression is not MemberExpression.
        /// </exception>
        /// <typeparam name="TEntity">Type of entity model.</typeparam>
        /// <typeparam name="TEnum">Type of enum.</typeparam>
        /// <param name="propertyExpression">Property expression to select member to check not equality.</param>
        /// <param name="value">Value of enum to compare with to check non-equality.</param>
        /// <param name="notEquals">Create not equals (!=) expression insetead of equals (==).</param>
        /// <returns>Lambda expression to check non-equality of property of enum type.</returns>
        private static Expression<Func<TEntity, bool>> EnumEquals<TEntity, TEnum>(
            Expression<Func<TEntity, TEnum>> propertyExpression,
            TEnum value,
            bool notEquals)
            where TEntity : class
            where TEnum : struct
        {
            if (propertyExpression == null)
                throw new ArgumentNullException(nameof(propertyExpression));

            MemberExpression memberExpression = propertyExpression.Body as MemberExpression;
            if (memberExpression == null)
                throw new ArgumentException(string.Format(
                    "{0} must be MemberExpression.", nameof(propertyExpression)));

            ParameterExpression parameterExpression = propertyExpression.Parameters.First();
            List<TEnum> values = new List<TEnum> { value };
            var containsMethod = values.GetType().GetMethod("Contains");
            var methodCall = Expression.Call(Expression.Constant(values), containsMethod, memberExpression);

            Expression compareExpression = methodCall;
            if (notEquals)
                compareExpression = Expression.Not(methodCall);

            var lambda = Expression.Lambda<Func<TEntity, bool>>(compareExpression, parameterExpression);
            return lambda;
        }
    }
}
