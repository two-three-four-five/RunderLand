using System;
using System.Collections.Generic;
using UnityEngine;

public class LimitedSizeQueue<T>
{
    private Queue<T> queue;
    private int maxSize;

    public LimitedSizeQueue(int maxSize)
    {
        this.queue = new Queue<T>();
        this.maxSize = maxSize;
    }

    public void Enqueue(T item)
    {
        if (queue.Count >= maxSize)
        {
            queue.Dequeue(); // Remove the oldest item from the front of the Queue
        }

        queue.Enqueue(item);
    }

    public T Dequeue()
    {
        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        return queue.Dequeue();
    }

    public T Peek()
    {
        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        return queue.Peek();
    }

    public int Count
    {
        get { return queue.Count; }
    }

    public bool IsEmpty
    {
        get { return queue.Count == 0; }
    }

    public Vector3 CalculateWeightedAverage()
    {
        if (queue.Count == 0)
        {
            throw new InvalidOperationException("Queue is empty.");
        }

        // Convert the Queue to an array to calculate the average
        T[] queueArray = queue.ToArray();

        // Initialize the sum of x, y, and z components
        float sumX = 0;
        float sumY = 0;
        float sumZ = 0;

        int queueSize = queueArray.Length;
        float totalWeight = 0;

        for (int i = 0; i < queueSize; i++)
        {
            // Assuming T is Vector3
            Vector3 vectorItem = (Vector3)Convert.ChangeType(queueArray[i], typeof(Vector3));

            // Calculate the weight based on the vector's position in the queue
            float weight = (i == 0) ? 0.1f : (i >= 1 && i <= 3) ? 0.2f : 0.3f;
            totalWeight += weight;

            // Sum the weighted x, y, and z components
            sumX += vectorItem.x * weight;
            sumY += vectorItem.y * weight;
            sumZ += vectorItem.z * weight;
        }

        // Calculate the weighted average of x, y, and z components
        float averageX = sumX / totalWeight;
        float averageY = sumY / totalWeight;
        float averageZ = sumZ / totalWeight;

        // Return the weighted average Vector3
        return new Vector3(averageX, averageY, averageZ);
    }
}
