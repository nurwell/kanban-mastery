# Debugging Exercise Report

## The Bug

### What Was Changed
In `KanbanApi/Program.cs`, the dependency injection registration was changed from:

```csharp
builder.Services.AddSingleton<IBoardService, BoardService>();
```

to:

```csharp
builder.Services.AddSingleton<BoardService>();
```

---

## Diagnosis

### Structured Debug Prompt Used

> "I'm getting this error when running my .NET 8 minimal API:
>
> `System.InvalidOperationException: Unable to resolve service for type 'KanbanApi.Services.IBoardService' while attempting to activate endpoint handler.`
>
> Here is my `Program.cs` registration:
>
> ```csharp
> builder.Services.AddSingleton<BoardService>();
> ```
>
> Here is my endpoint:
>
> ```csharp
> group.MapGet("/", async (string userId, IBoardService boardService) => { ... })
> ```
>
> The app starts and `dotnet build` succeeds with 0 errors. But the first request to `GET /boards` crashes with the exception above. I expected the `BoardService` to be injected."

### What the Error Means

The .NET dependency injection container is a lookup table. When an endpoint declares `IBoardService` as a parameter, the runtime asks the container:
*"Give me something registered as `IBoardService`."*

The container has `BoardService` registered — but only under its own concrete type key. It has no entry for the `IBoardService` key. The lookup fails and throws `InvalidOperationException`.

**Why the build succeeded:** The compiler only checks that types exist and method signatures match. It has no knowledge of what will or won't be registered at runtime. This class of bug is only detectable at runtime — on the first request to an affected endpoint.

---

## The Fix

```csharp
// Wrong — registers BoardService under its own type key only
builder.Services.AddSingleton<BoardService>();

// Correct — registers BoardService and maps it to IBoardService
builder.Services.AddSingleton<IBoardService, BoardService>();
```

The two-generic-argument form `AddSingleton<TService, TImplementation>()` tells the container:
- **TService** (`IBoardService`) — the key endpoints and services will ask for
- **TImplementation** (`BoardService`) — the concrete class to instantiate and return

---

## Why the Fix Works (In My Own Words)

The DI container works like a dictionary. When you register `AddSingleton<BoardService>()`, you are adding one entry with the key `BoardService`. When the endpoint asks for `IBoardService`, the container looks for a key named `IBoardService` — finds nothing — and crashes.

`AddSingleton<IBoardService, BoardService>()` adds the entry with the key `IBoardService` pointing to an instance of `BoardService`. Now when the endpoint requests `IBoardService`, the container finds the entry and injects the right object.

The interface is the contract. The concrete class is the implementation. The container needs to know which implementation fulfills which contract — that is exactly what the two-argument registration expresses.

---

## Key Takeaway

Always register services using the interface as the first type argument when your endpoints and services depend on interfaces, not concrete types. This is not just a DI technicality — it is what makes mocking and unit testing possible: you can swap `BoardService` for a `MockBoardService` in tests without touching a single endpoint.

---

## AI Collaboration Reflection

### How AI Was Used in This Exercise

The structured debug prompt in the Diagnosis section above was formed with Claude Code. I provided the exact error message, the broken registration line, and the endpoint code, then asked for an explanation of what went wrong and why.

### What the AI Did Well

Claude identified the root cause immediately: the DI container has no entry for `IBoardService` because only `BoardService` was registered as a concrete type. It explained the container-as-dictionary metaphor clearly and correctly identified why `dotnet build` succeeds while the first HTTP request crashes — a subtle distinction that is easy to miss.

### Where I Intervened

The AI's fix was one correct line. My intervention was in understanding and documenting it: I elevated the observation that this failure is **runtime-only, first-request-only** — the compiler cannot catch DI registration mismatches. The AI mentioned it briefly; I made it the central point because it is the most practically important thing to retain from this exercise. The "Key Takeaway" framing (interface as contract, concrete type as implementation, testability via swapping) is my own restructuring of the AI's more technical explanation into something more useful for future reference.
