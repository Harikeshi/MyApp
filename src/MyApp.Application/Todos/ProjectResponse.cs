using MyApp.Application.Abstractions;

namespace MyApp.Application.Todos;

public record ProjectResponse(
    int Id,
    string Name,
    string OwnerId,
    string OwnerName,
    bool IsOwner,
    IReadOnlyList<UserInfo> Members);
