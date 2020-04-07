using UnityEngine;

using Unity.Networking.Transport;
using System.Threading;
using System.Collections;

public class ClientBehaviour : MonoBehaviour
{
    [SerializeField]
    public ushort Port = 9000;
    public int MaxCountRequests = 100;
    public float IntervalBetweenRequests = 0.5f;

    private NetworkDriver localDriver;
    private NetworkConnection connection;
    private bool isDone = false;

    void Start()
    {
        localDriver = NetworkDriver.Create();
        connection = default(NetworkConnection);

        var endpoint = NetworkEndPoint.LoopbackIpv4;
        endpoint.Port = Port;
        connection = localDriver.Connect(endpoint);
    }

    public void OnDestroy()
    {
        localDriver.Dispose();
    }

    private int count = 0;
    void FixedUpdate()
    {
        localDriver.ScheduleUpdate().Complete();

        if (!connection.IsCreated)
        {
            if (!isDone)
            {
                Debug.Log("Something went wrong during connect");
            }
            return;
        }

        DataStreamReader stream;
        NetworkEvent.Type cmd;

        while ((cmd = connection.PopEvent(localDriver, out stream)) != NetworkEvent.Type.Empty)
        {
            if (cmd == NetworkEvent.Type.Connect)
            {
                Debug.Log($"We are now connected to the server.");
                StartCoroutine("SendInfo");
            }
            else if (cmd == NetworkEvent.Type.Data)
            {
                var value = stream.ReadString();
                Debug.Log(value);

                StartCoroutine("SendInfo");
                if (count > MaxCountRequests)
                {
                    Disconnect();
                }
            }
            else if (cmd == NetworkEvent.Type.Disconnect)
            {
                Disconnect();
            }

            void Disconnect() 
            {
                Debug.Log($"Client got disconnected from server.");
                isDone = true;
                connection.Disconnect(localDriver);
                connection = default(NetworkConnection);
            }
        }
    }

    IEnumerator SendInfo()
    {
        yield return new WaitForSeconds(IntervalBetweenRequests > 0 ? IntervalBetweenRequests : 0.5f);

        if (connection.IsCreated && localDriver.IsCreated)
        {
            var value = "{ \"success\":true }";
            var writer = localDriver.BeginSend(connection);
            writer.WriteString(value);
            localDriver.EndSend(writer);
            count++;
        }
    }
}