#include <stdio.h>

int main(void) {
    int x;
    if (scanf("%d", &x) != 1) {
        return 1;
    }
    printf("got %d\n", x * 2);
    return 0;
}
