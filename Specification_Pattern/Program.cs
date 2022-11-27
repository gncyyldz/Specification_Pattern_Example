
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;

#region Example
ApplicationDbContext context = new();

WomenAndOver18YearsOldOnlySpecification s1 = new();
PersonsOver18YearsOfAgeManufacturedBefore2020 s2 = new();
PeopleFitForTheJobSpecification s3 = new();
OnlyMenSpecification s4 = new();

var query = context.Persons
    .Where(s1.And(s2)
             .Or(s3)
             .And(s4)
             .ToExpression()
          )
    .ToQueryString();
Console.WriteLine(query);
#endregion

#region Entity - DbContext
abstract class Entity
{
    public int Id { get; set; }
}
class Person : Entity
{
    public string Name { get; set; }
    public int Age { get; set; }
    public Gender Gender { get; set; }
    public DateTime CreatedDate { get; set; }
}
enum Gender
{
    Woman,
    Man
}
class ApplicationDbContext : DbContext
{
    public DbSet<Person> Persons { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlServer("Server=localhost, 1433;Database=ApplicationDB2;User ID=SA;Password=1q2w3e4r+!;TrustServerCertificate=True");
    }
}
#endregion

#region Abstract Specification
abstract class Specification<T>
{
    public abstract Expression<Func<T, bool>> ToExpression();
    public bool IsSatisfiedBy(T entity)
    {
        Func<T, bool> predicate = ToExpression().Compile();
        return predicate(entity);
    }

    public Specification<T> And(Specification<T> specification)
        => new AndSpecification<T>(this, specification);
    public Specification<T> Or(Specification<T> specification)
        => new OrSpecification<T>(this, specification);
    public Specification<T> NotEqual(Specification<T> specification)
        => new NotEqualSpecification<T>(this, specification);
    public Specification<T> Equal(Specification<T> specification)
        => new EqualSpecification<T>(this, specification);
}
#endregion

#region Concrete Specifications
class PeopleFitForTheJobSpecification : Specification<Person>
{
    public override Expression<Func<Person, bool>> ToExpression()
    {
        return p => p.Age > 18;
    }
}
class OnlyMenSpecification : Specification<Person>
{
    public override Expression<Func<Person, bool>> ToExpression()
    {
        return p => p.Gender == Gender.Man;
    }
}
class WomenAndOver18YearsOldOnlySpecification : Specification<Person>
{
    public override Expression<Func<Person, bool>> ToExpression()
    {
        return p => p.Gender == Gender.Woman && p.Age > 18;
    }
}
class PersonsOver18YearsOfAgeManufacturedBefore2020 : Specification<Person>
{
    public override Expression<Func<Person, bool>> ToExpression()
    {
        return p => p.CreatedDate.Year <= 2020 && p.Age > 18;
    }
}
#endregion

#region Composite Specifications
class ParameterReplacer : ExpressionVisitor
{
    private readonly ParameterExpression _parameter;
    public ParameterReplacer(ParameterExpression parameter)
        => _parameter = parameter;
    protected override Expression VisitParameter(ParameterExpression node)
        => base.VisitParameter(_parameter);
}

#region Logical Specifications
class AndSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public AndSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        Expression<Func<T, bool>> leftExpression = _left.ToExpression();
        Expression<Func<T, bool>> rightExpression = _right.ToExpression();

        ParameterExpression parameterExpression = Expression.Parameter(typeof(T));
        BinaryExpression andExpression = Expression.AndAlso(leftExpression.Body, rightExpression.Body);
        andExpression = (BinaryExpression)new ParameterReplacer(parameterExpression).Visit(andExpression);

        return Expression.Lambda<Func<T, bool>>(andExpression, parameterExpression);
    }
}
class OrSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public OrSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        Expression<Func<T, bool>> leftExpression = _left.ToExpression();
        Expression<Func<T, bool>> rightExpression = _right.ToExpression();

        ParameterExpression parameterExpression = Expression.Parameter(typeof(T));
        BinaryExpression orExpression = Expression.OrElse(leftExpression.Body, rightExpression.Body);
        orExpression = (BinaryExpression)new ParameterReplacer(parameterExpression).Visit(orExpression);

        return Expression.Lambda<Func<T, bool>>(orExpression, parameterExpression);
    }
}
class NotEqualSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public NotEqualSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        Expression<Func<T, bool>> leftExpression = _left.ToExpression();
        Expression<Func<T, bool>> rightExpression = _right.ToExpression();

        ParameterExpression parameterExpression = Expression.Parameter(typeof(T));

        BinaryExpression notEqualExpression = Expression.NotEqual(leftExpression.Body, rightExpression.Body);
        notEqualExpression = (BinaryExpression)new ParameterReplacer(parameterExpression).Visit(notEqualExpression);

        return Expression.Lambda<Func<T, bool>>(notEqualExpression, parameterExpression);
    }
}
class EqualSpecification<T> : Specification<T>
{
    private readonly Specification<T> _left;
    private readonly Specification<T> _right;

    public EqualSpecification(Specification<T> left, Specification<T> right)
    {
        _left = left;
        _right = right;
    }

    public override Expression<Func<T, bool>> ToExpression()
    {
        Expression<Func<T, bool>> leftExpression = _left.ToExpression();
        Expression<Func<T, bool>> rightExpression = _right.ToExpression();

        ParameterExpression parameterExpression = Expression.Parameter(typeof(T));

        BinaryExpression equalExpression = Expression.Equal(leftExpression.Body, rightExpression.Body);
        equalExpression = (BinaryExpression)new ParameterReplacer(parameterExpression).Visit(equalExpression);

        return Expression.Lambda<Func<T, bool>>(equalExpression, parameterExpression);
    }
}
#endregion
#endregion














