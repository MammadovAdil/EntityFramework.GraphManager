# Introduction

**Automatically** handle and define state of *complex hierarchical* graph of entities for *EntityFramework Code-First*.

## Prerequisite

- This API should be used with EntityFramewok Code-First
- You should be familiar with Fluent API ([quick introduction](http://stackoverflow.com/documentation/entity-framework/4530/code-first-fluent-api/15861/mapping-models#t=201611100620522234993)).
- Add *explicit* foreign key properties to your models. If we do not add explicit property and configure it as foreign key, then entity framework will create it for us. But, we have to create it ourselves.
```
public class Post
{
    // Foreign key to Blog must exist
    public int BlogID { get; set; }
    public Blog Blog { get; set; }
}
```
- Additionally, if you have *many-to-many* relationships, you should create model for third table. For example, if you have *Student* and *Course* models, and if there is many-to-many relation between them, you should also create model for relating *third table*, which porbably will be *StudentCourse*. Then *Student and StudentCourse*, *Course and StudentCourse* models will have *one-to-many* relationships. This will help a lot when we want to change which Students attend which Courses (add, update or delete.).

## Features:

- Automatically *define state* of entity graph with ease.
- Use not only primary keys, but also configured **unique keys** to define state.
- Simple and complex unique keys.
- Send update query *for only changed* properties.
- Handle entity duplications according to primary and unique keys.
- Familiar Fluent API style mappings.
- Additional customization options to not update certain properties which shouldnot change.
- Manual operations *after automatic state define*.

## Installation:

Install from nuget:

`Install-Package Ma.EntityFramework.GraphManager`

## Usage:

1. Your mapping classes should intherit `ExtendedEntityTypeConfiguration<TEntity>`, where `TEntity` is type of entity 
which you are configuring mappings for. To be able to do so, you have to add 
**Ma.EntityFramework.GraphManager.CustomMappings** namespace to unsings section. 
Remember that, *you do not have to inherit this configuration class* if you do not need any custom mappings (i.e. unique keys, not updated properties and etc.). 
Automatic state defining should still work without this.
2. Add **Ma.EntityFramework.GraphManager** to your usings section where to you want to add or update entities.
3. Define state of whole graph using just one line: `context.AddOrUpdate(entity);`

### Further reading:

- Be sure to check out **Wiki** section soon for detailed documentation.
- Check out [Code Project article](http://www.codeproject.com/Articles/1153391/Automatic-graph-operations-using-EntityFramework)
for step-by-step sample application and more.
