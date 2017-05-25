                      Драйвер работы с портами GPIO Raspberry Pi3 (2, B+ не тестировались)
     
   Драйвер использует для работы библиотеку WiringPi, необхлдимо установить, если она не установлена по умолчанию.
      
   Для настройки используемых GPIO в Rapid SCADA, а так же для настройки направления работы (IN или OUT), настройки 
   подтягивающих резисторов (Pud_UP или Pud_down), уровня выхода (Low или High) перед активацией GPIO в режиме OUT,
   типа нумерации (wPi или BCM) используется командная строка Коммуникатора.
      
   Параметры командной строки - числа, которые являются битовыми масками для определения состояния вкл/выключен.
   Параметр 1 - битовая маска используемых каналов GPIO (нумерация каналов соответствует BCM нумерации с 4 по 27,
   с 0 по 3 не используется).
   Параметр 2 - битовая маска направления GPIO - 0 = вход, 1 = выход
   Параметр 3 - битовая маска использования подтягивающих резисторов процессора (отключение подтягивающих резисторов
   в данной версии драйвера не реализовано, 0 = подтяжка к массе, 1 = подтяжка к плюсу)
   Параметр 4 - битовая маска активации уровня выхода, если требуется 0 = Low (низкий уровень), 1 = High (высокий 
   уровень). Требуется для активации высоким уровнем перед инициализацией выхода при использовании релейной платы,
   управляемой минусом.
   Параметр 5 - набор команд.
   Bit 0 числа - 1 разрешает записывать уровень перед активацией выхода, 0 не записывать уровнь перед активацией выхода
   Bit 1 числа - 1 разрешает сохранение уровня выхода при перезапуске Scada Коммуникатора. Создается файл параметров в 
   папке ScadaComm\Log, папка должна находиться в tmpfs, согласно настроек Scada.
   При изменении параметров командной стройки и перезапуске ScadaComm произойдет полная инициализация, при установленном
   бите 0, независимо от выставленного бита 1. 
   Bit 2 числа - выбор формата нумерации GPIO (BCM или WirngPi) в файле kpXXX.txt, где XXX номер вашего KP в базе данных 
   сервера.
      
   Для удобства настроек запустите Свойства КП на вкладке Опрос КП соответствующей линии Коммуникатора.
   Линия связи - Параметры линии связи - Канал связи - Тип = Не задан
   Скопируйте параметры командной строки в ScadaCommSvcConfig.xml соответствующей линии на Raspberry Pi при изменении.


                                                       Online translate
                                                       
                          The driver operate the GPIO ports of the Raspberry Pi3 (2, B+ not tested)

The driver uses the WiringPi library, neobhodimo to install if it is not installed by default.

To configure GPIO used in Rapid SCADA and direction (IN or OUT), settings 
pull-up resistors (Pud_UP or Pud_down), output level (Low or High) before activating the GPIO in OUT mode,
the type of numbering (wPi or BCM) using the command line of your device.

 Command-line options, which are bit masks to determine the state of on/off.
Option 1 - bitmask of used channels GPIO (numbering of the channels corresponds to BCM numbering from 4 to 27, 0 to 3 not used).
The parameter 2 bit mask of the GPIO direction - 0 = input, 1 = output
Option 3 - a bitmask of using pull-up resistors CPU (disable pullup resistors in this version of the driver is not implemented, 0 = lift to ground, 1 = tightening to plus)
 Option 4 - a bitmask of the activated output level, if required 0 = low (low level) 1 = High (high level). Required to activate high level before initializing the output using a relay Board that is controlled minus.
Option 5 - command set.
Bit 0 of the number - 1 allows to record the level before the activation of the output, 0 to record levels before the activation of the output
1 Bit number - 1 allows saving of the output level when you restart the Scada Device. Settings file is created in the folder ScadaComm\Log folder should be located in tmpfs, according to the settings Scada.
 When you change the settings of command prompt and restart ScadaComm there will be a full initialization, when the bit is set to 0, regardless of the set bit is 1. 
Bit 2 - select the numbering format GPIO (BCM or WirngPi) in the file kpXXX.txt where XXX is the number of your KP in the server database.

For convenience, the settings, run the CP Properties Poll tab KP corresponding line of the Communicator.
The communication Parameters of the communication line - communication Channel - Type = Not specified
Copy the command-line parameters to ScadaCommSvcConfig.xml the corresponding line on the Raspberry Pi when changing.
