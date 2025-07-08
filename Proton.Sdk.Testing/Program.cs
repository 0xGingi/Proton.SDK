using Proton.Sdk.Drive;

namespace Proton.Sdk.Testing;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Proton Drive Client Test");
        var sessionBeginRequest = new SessionBeginRequest
        {
            Username = "user@protonmail.com",
            Password = "password",
            Options = new() { AppVersion = "macos-drive@1.0.0-alpha.1+rclone" },
        };
        Console.WriteLine("Starting session with username: " + sessionBeginRequest.Username);
        var cancellationToken = CancellationToken.None;

        var session = await ProtonApiSession.BeginAsync(sessionBeginRequest, cancellationToken);
        if (session is null)
        {
            Console.WriteLine("Failed to start session.");
            return;
        }

        if (session.IsWaitingForSecondFactorCode)
        {
            Console.WriteLine("Input your two factor code: ");
            string two_factor = Console.ReadLine();
            if (two_factor is null)
            {
                two_factor = string.Empty;
            }

            await session.ApplySecondFactorCodeAsync(two_factor, cancellationToken);
        }

        var client = new ProtonDriveClient(session);
        Console.WriteLine("Creating new ProtonDriveClient instance.");
        var volumes = await client.GetVolumesAsync(cancellationToken);
        if (volumes.Length == 0)
        {
            Console.WriteLine("No volumes found.");
            return;
        }

        Console.WriteLine($"Found {volumes.Length} volume(s).");
        Console.WriteLine($"Volume size: {volumes[0].MaxSpace}");
        Console.WriteLine("Volume ID: " + volumes[0].Id);
        var mainVolume = volumes[0];
        var share = await client.GetShareAsync(mainVolume.RootShareId, cancellationToken);
        await CheckFolderChildrenRecursiveAsync(client, share, mainVolume, cancellationToken);
    }

    private static async Task CheckFolderChildrenRecursiveAsync(
        ProtonDriveClient client,
        Share share,
        Volume volume,
        CancellationToken token,
        string currentPath = "")
    {
        var children = client.GetFolderChildrenAsync(
            new NodeIdentity(share.ShareId, volume.Id, share.RootNodeId),
            token);

        await foreach (var child in children)
        {
            var childPath = string.IsNullOrEmpty(currentPath) ? child.Name : $"{currentPath}/{child.Name}";
            if (child is FolderNode folder)
            {
                await CheckFolderChildrenRecursiveAsync(client, share, folder, token, childPath);
            }
            else
            {
                Console.WriteLine(childPath);
            }
        }
    }

    private static async Task CheckFolderChildrenRecursiveAsync(
        ProtonDriveClient client,
        Share share,
        FolderNode node,
        CancellationToken token,
        string currentPath)
    {
        var children = client.GetFolderChildrenAsync(
            new NodeIdentity(share.ShareId, node.NodeIdentity.VolumeId, node.NodeIdentity.NodeId),
            token);
        Console.WriteLine("Node Identity Volume ID: " + node.NodeIdentity.VolumeId);
        Console.WriteLine("Node Identity Node ID: " + node.NodeIdentity.NodeId);

        await foreach (var child in children)
        {
            var childPath = string.IsNullOrEmpty(currentPath) ? child.Name : $"{currentPath}/{child.Name}";
            if (child is FolderNode folder)
            {
                await CheckFolderChildrenRecursiveAsync(client, share, folder, token, childPath);
            }
            else
            {
                Console.WriteLine(childPath);
            }
        }
    }
}
