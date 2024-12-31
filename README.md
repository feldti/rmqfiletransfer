# rmqfiletransfer

Ein Tool zum Transferieren von Dateien über RabbitMQ. Das hat den Vorteil, das keine Dateisysteme freigegeben werden müssen oder 
über Samba oder NFs gemountet werden müssen.

Das Tool versendet eine Datei an einen Empfänger. Ist dieser nicht gestartet, dann geht der Transfer verloren. Das Tool zerstückelt die
Datei in mehrere kleinere Pakete, damit RMQ nicht durch riesige nachrichten zu stark beeinrächtigt werden.

Das Tool kann als Sender oder als Empfänger mit Topic gestartet werden. Er kann auch als wiederholender Sender gestartet werden. Dann schaut 
der Prozess nach, ob eine Datei an einem Pfad vorkommt und sendet diese dann.

## Transfer im Detail

Ein Transfer einer Datei erfolgt über RabbitMQ. Dazu muß ein Prozess auf dem Quellrechner laufen und ein Prozess auf dem Zielrechner.
Der Sender kann temporär gestartet werden, der Empfänger sollt dagegen immerlaufen, damit keine großen Datensätze unter RabbitMQ gespeichert werden müssen.

Der Sender schnappt sich eine Datei und teilt diese in n-1 kleine Packungen von der Größe x-KByte auf (Default x = 100). Das letzte Paket hat dann eine Größe < X-KByte. 

Dateien werden immer komprimiert vor der Bearbeitung außer ihre Dateierweiterung ist Mitglied einer Liste von Extensions: zip, 7z, tar.gz, tgz.

Jede Sendung bekommt die folgenden Informationen:

* Job-ID
* die Default-Paketgröße (um die Ablageposition des aktuellen Paketes ermitteln zu können - z.Z. 100 KByte)
* die Größe der abzulegenden Datei (ggfs. komprimiert, umd die Pakete ablegen zu können)
* einen Sendeindex (angefangen mit 1, endet mit n)
* die Größe der aktuellen Sendung ist die Anzahl der Bytes im MessageBody
* temp. Dateiname zur Ablage (angefangen mit "_____" (5x) und zusätzlich der Job-ID mit .part Erweiterung)
* endgültiger Dateiname zur Ablage (Name für Umbenennung)
* entpacken nach vollständigem Empfang (default: true)
* die Anzahl der notwendigen Pakete (n)
* File Creation Timestamp
* File Modification Timestamp

### Beispiel: Transfer

`rmqfiletransfer sendfile --file=test.pdf --mqrkey=test`

### Beispiel: Receive

`rmqfiletransfer receivefiles --directory=/home/usr/testreceive --mqrkey=test`

