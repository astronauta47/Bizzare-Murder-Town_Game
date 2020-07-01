using UnityEngine;
using System.IO.Ports;
using System.Collections;

public class Test : MonoBehaviour
{
    SerialPort serialPort = new SerialPort("COM5", 9600);

    // Start is called before the first frame update
    void Start()
    {
        serialPort.Open();
        serialPort.ReadTimeout = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if(serialPort.IsOpen)
        {
            try
            {
                serialPort.Write("1");
                int i = serialPort.ReadByte();

                if (i == 1 && !GetComponent<AudioSource>().isPlaying)
                {
                    GetComponent<AudioSource>().Play();
                }
                else if (i == 2)
                {
                    GetComponent<AudioSource>().Stop();
                }

            }
            catch (System.Exception)
            {

                //throw;
            }
        }
            
    }
}
