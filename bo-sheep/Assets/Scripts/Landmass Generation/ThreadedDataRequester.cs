using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class ThreadedDataRequester : MonoBehaviour {

	// Need a static instance that we can refer to in the static RequestData
	// method below
	static ThreadedDataRequester instance;
	Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();

	void Awake() {
		instance = FindObjectOfType<ThreadedDataRequester> ();
	}

	// Method for triggering generation of data in a separate thread. generateData
	// is the method to call to generate the data and callback is what we'll call
	// when the data has been generated but note that the callback can't be called
	// from inside the thread as that callback would then also be executed in the
	// thread and we can't have that because the callback is going to be doing
	// stuff that Unity only wants done in the main thread.  So instead, when map
	// data becomes available, the callback is put on a queue of callbacks and the
	// main thread (in the form of the Update method) monitors the queue and calls
	// any callbacks on it
	public static void RequestData(Func<object> generateData, Action<object> callback) {
		ThreadStart threadStart = delegate {
			instance.DataThread (generateData, callback);
		};

		new Thread (threadStart).Start ();
	}

	void DataThread(Func<object> generateData, Action<object> callback) {
		object data = generateData ();

		// Data generation complete - lock the queue and put the callback up in
		// there, yo
		lock (dataQueue) {
			dataQueue.Enqueue (new ThreadInfo (callback, data));
		}
	}

	void Update() {
		// If we've any HeightMap callbacks queued up, execute them
		if (dataQueue.Count > 0) {
			for (int i = 0; i < dataQueue.Count; i++) {
				ThreadInfo threadInfo = dataQueue.Dequeue ();

				threadInfo.callback (threadInfo.parameter);
			}
		}
	}

	// Gonna use this thing for both HeightMap and MeshData generating
	// threads so define it with a generic type
	struct ThreadInfo {
		public readonly Action<object> callback;
		public readonly object parameter;

		public ThreadInfo (Action<object> callback, object parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}
	}

}
