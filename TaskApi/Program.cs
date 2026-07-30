var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

app.UseCors("AllowAll");
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/api-info", () => new
{
    name = "Task API",
    version = "1.0",
    endpoints = new[] { "/tasks" }
});

app.MapGet("/health", () => new { status = "ok" });

var tasks = new List<TaskItem>
{
    new TaskItem(1, "Buy milk", false),
    new TaskItem(2, "Finish backend task", true),
    new TaskItem(3, "Push to GitHub", false)
};

app.MapGet("/tasks", () => tasks);

app.MapGet("/tasks/{id:int}", (int id) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    return task is not null ? Results.Ok(task) : Results.NotFound(new { error = $"Task {id} not found" });
});

app.MapPost("/tasks", (UpsertTaskDto dto) =>
{
    if (string.IsNullOrWhiteSpace(dto.Title))
    {
        return Results.BadRequest(new { error = "Task title cannot be empty" });
    }

    var newId = tasks.Count > 0 ? tasks.Max(t => t.Id) + 1 : 1;
    var newTask = new TaskItem(newId, dto.Title.Trim(), false);
    tasks.Add(newTask);

    return Results.Created($"/tasks/{newId}", newTask);
});

app.MapPut("/tasks/{id:int}", (int id, UpsertTaskDto dto) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) return Results.NotFound(new { error = $"Task {id} not found" });

    if (string.IsNullOrWhiteSpace(dto.Title))
    {
        return Results.BadRequest(new { error = "Task title cannot be empty" });
    }

    var updatedTask = task with { Title = dto.Title.Trim(), Done = dto.Done };
    tasks[tasks.IndexOf(task)] = updatedTask;

    return Results.Ok(updatedTask);
});

app.MapDelete("/tasks/{id:int}", (int id) =>
{
    var task = tasks.FirstOrDefault(t => t.Id == id);
    if (task is null) return Results.NotFound(new { error = $"Task {id} not found" });

    tasks.Remove(task);
    return Results.NoContent();
});

app.Run();

record TaskItem(int Id, string Title, bool Done);
record UpsertTaskDto(string Title, bool Done);