namespace MiniPdm.Application.Services;

public sealed class CycleDetector : ICycleDetector
{
    public bool DetectCycle(
        Guid rootId,
        Func<Guid, IEnumerable<Guid>> childrenProvider,
        out IReadOnlyList<Guid> cyclePath)
    {
        var visited = new HashSet<Guid>();
        var recursionStack = new HashSet<Guid>();
        var currentPath = new List<Guid>();

        bool Dfs(Guid current)
        {
            visited.Add(current);
            recursionStack.Add(current);
            currentPath.Add(current);

            var children = childrenProvider(current) ?? Enumerable.Empty<Guid>();
            foreach (var child in children)
            {
                if (recursionStack.Contains(child))
                {
                    // Цикл найден
                    var cycleStartIdx = currentPath.IndexOf(child);
                    var path = currentPath.Skip(cycleStartIdx).ToList();
                    path.Add(child);
                    currentPath.Clear();
                    currentPath.AddRange(path);
                    return true;
                }

                if (!visited.Contains(child))
                {
                    if (Dfs(child))
                        return true;
                }
            }

            recursionStack.Remove(current);
            currentPath.RemoveAt(currentPath.Count - 1);
            return false;
        }

        if (Dfs(rootId))
        {
            cyclePath = currentPath;
            return true;
        }

        cyclePath = Array.Empty<Guid>();
        return false;
    }

    public bool WouldCreateCycle(
        Guid parentId,
        Guid childId,
        Func<Guid, IEnumerable<Guid>> childrenProvider)
    {
        if (parentId == childId)
            return true;

        // Если из childId можно дойти до parentId, добавление связи создаст цикл
        var visited = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(childId);
        visited.Add(childId);

        while (queue.Count > 0)
        {
            var curr = queue.Dequeue();
            if (curr == parentId)
                return true;

            var children = childrenProvider(curr) ?? Enumerable.Empty<Guid>();
            foreach (var child in children)
            {
                if (child == parentId)
                    return true;

                if (visited.Add(child))
                {
                    queue.Enqueue(child);
                }
            }
        }

        return false;
    }
}
