py -3.12 -m venv mvenv
call ./mvenv/Scripts/activate
pip install -r requirements.txt
pip freeze > requirements.txt
python faster-whisper_server.py