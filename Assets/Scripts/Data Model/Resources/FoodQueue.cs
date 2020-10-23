using Priority_Queue;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// There should only be one of each type of food in the food queue, for food that has amount > 0 available.
/// </summary>
public class FoodQueue
{
    public static FoodQueue Instance;
    public static SimplePriorityQueue<Food> foodQueue;

    public FoodQueue()
    {
        foodQueue = new SimplePriorityQueue<Food>();
    }

    public void Enqueue(Food food)
    {
        //Debug.Log("FoodQueue :: Enqueue");        
        foodQueue.Enqueue(food, 1/food.calories);
        // Possible optimization problem due to double type?
    }
    public Food Dequeue()
    {
        //Debug.Log("FoodQueue :: Dequeue");
        return foodQueue.Dequeue();
    }
    public Food First()
    {
        if (foodQueue.Count == 0)
            return null;

        return foodQueue.First;
    }

    public bool Contains(Food food)
    {
        bool check = foodQueue.Contains(food);
        //Debug.Log("FoodQueue :: Contains - " + check + " for " + food);
        return check;
    }
    public void Remove(Food food)
    {
        foodQueue.Remove(food);
    }
}
