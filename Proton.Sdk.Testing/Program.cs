using System.Text;
using Proton.Sdk.Drive;
using DotNetEnv;

namespace Proton.Sdk.Testing;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Load .env if it exists
        if (File.Exists(".env"))
        {
            Env.Load();
        }

        string? username = Environment.GetEnvironmentVariable("PROTON_USERNAME");
        string? password = Environment.GetEnvironmentVariable("PROTON_PASSWORD");

        if (string.IsNullOrWhiteSpace(username))
        {
            Console.Write("Enter your Proton username: ");
            username = Console.ReadLine() ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            Console.Write("Enter your Proton password: ");
            password = Console.ReadLine() ?? string.Empty;
        }

        // Save to .env if needed
        if (!File.Exists(".env") || 
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PROTON_USERNAME")) ||
            string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PROTON_PASSWORD")))
        {
            File.WriteAllLines(".env", new[]
            {
                $"PROTON_USERNAME={username}",
                $"PROTON_PASSWORD={password}"
            });
        }

        Console.WriteLine("Proton Drive Client Test");
        var sessionBeginRequest = new SessionBeginRequest
        {
            Username = username,
            Password = password,
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

        await session.ApplyDataPasswordAsync(Encoding.UTF8.GetBytes(password), CancellationToken.None);

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
        var rootNodeIdentity = new NodeIdentity(share.ShareId, mainVolume.Id, share.RootNodeId);
        // Start monitoring file revisions every 30 seconds
        await MonitorFileRevisionsAsync(client, rootNodeIdentity, cancellationToken, share, mainVolume);
        await CheckFolderChildrenRecursiveAsync(client, share, mainVolume, cancellationToken);
    }

    private static async Task MonitorFileRevisionsAsync(ProtonDriveClient client, NodeIdentity nodeIdentity, CancellationToken token,
        Share share, Volume volume)
    {
        var seenRevisionIds = new HashSet<string>();
        while (true)
        {
            bool foundUpdate = false;
            try
            {
                var revisions = await client.GetFileRevisionsAsync(nodeIdentity, token);
                foreach (var revision in revisions)
                {
                    var revise = revision;
                    Console.WriteLine("Revision found");
                    foundUpdate = true;

                    var file = await client.GetNodeAsync(share.ShareId, revision.FileId, CancellationToken.None);
                    if (file is FolderNode folder)
                    {
                        Console.WriteLine("Folder found");
                    } else if (file is FileNode fileNode)
                    {
                        Console.WriteLine($"File that got changed: {file.Name}, to state {file.State}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching revisions: {ex.Message}");
            }
            if (!foundUpdate)
            {
                Console.WriteLine("nothing");
            }
            await Task.Delay(TimeSpan.FromSeconds(10), token);
        }
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
