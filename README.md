# Как запустить

На компьютере должно быть установлено платформа .NET 8 SDK и Python версии 3.10+

1) Сначала запускаем сервер на Python.  
В терминале заходим в папку SpeedTestServer и вводим:  
pip install -r requirements.txt  
python server.py --host 0.0.0.0 --port 8000

2) Запускаем приложение на WPF.  
В терминале в папке SystemInfoApp вводим:  
dotnet restore  
dotnet build -c Release  
dotnet run