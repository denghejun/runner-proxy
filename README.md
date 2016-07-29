## 说明
1. 该组件用于对method的调用控制，支持一般调用、重试次数调用、重试最大失败本地缓存调用三种功能。
2. 该组件多线程安全
3. 本地缓存处理线程默认会在第一个失败缓存文件产生时，如果要手动指定启动缓存处理线程的参数，需要提前手动调用
   Runner.StartProcessCacheRunners(CacheProcessOption option)
4. 缓存文件默认存在应用程序运行目录//RunnerLocalCache文件夹下，缓存文件为json格式（yyyy-mm-dd.txt）
