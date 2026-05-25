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
		if (instance == null) {
			instance = this;
		} else if (instance != this) {
			Destroy(gameObject);
		}
	}

	public static void RequestData(Func<object> generateData, Action<object> callback) {
		if (instance == null) {
			Debug.LogError("[ThreadedDataRequester] Instance is null! Make sure there is a ThreadedDataRequester in the scene.");
			return;
		}

		ThreadStart threadStart = delegate {
			try {
				instance.DataThread (generateData, callback);
			} catch (Exception e) {
				lock (instance.dataQueue) {
					instance.dataQueue.Enqueue(new ThreadInfo((obj) => {
						Debug.LogError($"[ThreadedDataRequester] Thread error: {e}");
					}, null));
				}
			}
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
		lock (dataQueue) {
			if (dataQueue.Count > 0) {
				while (dataQueue.Count > 0) {
					ThreadInfo threadInfo = dataQueue.Dequeue ();
					threadInfo.callback (threadInfo.parameter);
				}
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
