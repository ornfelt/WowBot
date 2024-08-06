
#!/bin/bash

# Define variables
SRC_DIR="$HOME/Code2/C#/WowBot/java"
JAR_DIR="$HOME/Downloads/jar_files"
CLASSPATH="$JAR_DIR/jna-5.13.0.jar:$JAR_DIR/jna-platform-5.13.0.jar:$JAR_DIR/mariadb-java-client-3.2.0.jar:$JAR_DIR/mysql/mysql-connector-j-9.0.0/mysql-connector-j-9.0.0.jar"

# Navigate to the source directory
cd $SRC_DIR

# Compile the Java files
#javac -cp $CLASSPATH *.java
# Or:
#FILES=$(ls *.java | grep -v WowTabFinder_windows.java)
#javac -cp $HOME/Downloads/jar_files/jna-5.13.0.jar:$HOME/Downloads/jar_files/jna-platform-5.13.0.jar:$HOME/Downloads/jar_files/mariadb-java-client-3.2.0.jar:$HOME/Downloads/jar_files/mysql/mysql-connector-j-9.0.0/mysql-connector-j-9.0.0.jar $FILES
# Better:
javac -d . -cp $CLASSPATH $(find . -name "*.java" ! -name "WowTabFinder_windows.java")

# Create the MANIFEST.MF file
cat > MANIFEST.MF <<EOL
Manifest-Version: 1.0
Main-Class: wowbot.Main
Class-Path: . /home/jonas/Downloads/jar_files/jna-5.13.0.jar /home/jonas/Downloads/jar_files/jna-platform-5.13.0.jar /home/jonas/Downloads/jar_files/sql-jars/mariadb-java-client-3.2.0.jar /home/jonas/Downloads/jar_files/mysql/mysql-connector-j-9.0.0/mysql-connector-j-9.0.0.jar
EOL

# Create the JAR file
#jar cfm WowBot.jar MANIFEST.MF *.class
jar cfm WowBot.jar MANIFEST.MF $(find wowbot -name "*.class")

# Run jar
java -jar WowBot.jar 1 1

