<html>
	<head>
		<meta http-equiv="Content-Language" content="de">
		<meta http-equiv="Content-Type" content="text/html; charset=utf-8">
		<title>Sch&uuml;lervertretungspl&auml;ne</title>
	</head>
	
	<body>
		<p><h1><font face="Sans-Serif"><center><i>Vertretungspl&auml;ne f&uuml;r Sch&uuml;lerInnen</i></center></font></h1></p>
		<table border="2" cellpadding="0" cellspacing="0" style="border-collapse: collapse" bordercolor="#555555" width="100%" height="80">
			<tr>
				<?php
					//Leeren eventueller Zwischenspeicherungen bzgl. Vorhandensein von Dateien
					clearstatcache();
					//*** Konstanten ***
					$urlBase = "https://heriburg-gymnasium.de/images/plaene/";

					//Prüfe vorhandene Dateien
					$existingFiles = getExistingFilenames();
					
					for($i = 0; $i < count($existingFiles); $i++) {
						print "<td width =\"".(100 / count($existingFiles))."%\" align=\"center\" height=\"80\"><font face=\"Arial\" size=\"4\"><a href=\"".$urlBase.explode(";", $existingFiles[$i])[0]."\">".explode(";", $existingFiles[$i])[1]."</a></font></td>\n";							
					}
					
					function checkForExistance($file) {
						if(file_exists(getcwd()."/".$file))
							return true;
						return false;
					}
				
					function generateName($index) {
						$daysOfWeek = array("Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag");
						$types = array("heute", "morgen");
						return "schuelerplan_".substr(strtolower($daysOfWeek[$index % 5]), 0, 2)."_".$types[floor($index / 5)].".html;".$daysOfWeek[$index % 5];
					}
					
					function getExistingFilenames() {
						$existingFiles = array();
						for($i = 0; $i < 10; $i++) {
							$tbc = generateName($i);
							if(checkForExistance(explode(";", $tbc)[0])) {
								array_push($existingFiles, $tbc);
								// Die Datei für den aktuellen Typ wurde gefunden, springe zum nächsten Typ bzw. Ende
								$i = 5 * (floor($i / 5) + 1) - 1;
							}
						}
						return $existingFiles;
					}
				?>
			</tr>
		</table>
	</body>
</html>
