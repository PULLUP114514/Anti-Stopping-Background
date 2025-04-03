#include <iostream>
#include <fstream>
#include <string>
using namespace std;
void modifyStatusFile(const string& filename) {
    //ifstream infile(filename);
    //if (!infile) {
    //    return;
    //}
    //string content;
    //getline(infile, content);
    //infile.close();

    //if (content == "sign wait for reboot") {
    //    content = "sign rebooted";
    //}
    //else {
    //    return;
    //}

    ofstream outfile(filename);
    if (!outfile) {
        return;
    }
    outfile << "sign rebooted";
    outfile.close();
}

int main() {
    modifyStatusFile("c:\\Anti-sb-status.cfg");
	cout << "请再次打开Classisland!（按下回车后退出）" << endl;
    //char a;
    //cin>>a;
    return 0;
}