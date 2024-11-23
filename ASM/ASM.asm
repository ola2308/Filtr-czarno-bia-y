.data
    align 16
    rgb_weights REAL4 0.299, 0.587, 0.114, 0.0

.code
GrayscaleFilter PROC
    ; Parametry:
    ; rcx - wskaŸnik do inputBuffer
    ; rdx - wskaŸnik do outputBuffer
    ; r8d - pixelCount (int)
    ; xmm3 - brightness (float)

    ; Function prologue
    push rbp
    mov rbp, rsp

    ; Zapisz rejestry nieulotne
    push rbx
    push r12
    push r13
    push r14
    push r15

    ; Przechowaj parametry
    mov r12, rcx        ; Input buffer
    mov r13, rdx        ; Output buffer
    mov r14, r8         ; Pixel count
    movss xmm15, xmm3   ; Brightness factor

    ; Za³aduj wagi RGB i zastosuj jasnoœæ
    movups xmm14, [rgb_weights]    ; Za³aduj wszystkie cztery wagi
    shufps xmm15, xmm15, 0         ; Rozszerz jasnoœæ do wszystkich elementów
    mulps xmm14, xmm15             ; Zastosuj jasnoœæ do wag

    ; Oblicz liczbê pe³nych bloków SIMD (4 piksele na blok)
    mov rax, r14
    shr rax, 2                ; rax = pixelCount / 4
    mov r15, rax              ; Liczba bloków SIMD

    ; Oblicz pozosta³e piksele
    and r14, 3                ; r14 = pixelCount % 4

    ; Jeœli s¹ bloki SIMD do przetworzenia
    cmp r15, 0
    je process_remaining_pixels

process_simd_loop:
    ; Przetwarzaj 4 piksele jednoczeœnie

    ; Wczytaj komponenty B
    movzx eax, BYTE PTR [r12]        ; B0
    movzx ebx, BYTE PTR [r12 + 3]    ; B1
    movzx ecx, BYTE PTR [r12 + 6]    ; B2
    movzx edx, BYTE PTR [r12 + 9]    ; B3
    movd xmm0, eax
    pinsrd xmm0, ebx, 1
    pinsrd xmm0, ecx, 2
    pinsrd xmm0, edx, 3
    cvtdq2ps xmm0, xmm0              ; Konwertuj na float

    ; Wczytaj komponenty G
    movzx eax, BYTE PTR [r12 + 1]    ; G0
    movzx ebx, BYTE PTR [r12 + 4]    ; G1
    movzx ecx, BYTE PTR [r12 + 7]    ; G2
    movzx edx, BYTE PTR [r12 + 10]   ; G3
    movd xmm1, eax
    pinsrd xmm1, ebx, 1
    pinsrd xmm1, ecx, 2
    pinsrd xmm1, edx, 3
    cvtdq2ps xmm1, xmm1              ; Konwertuj na float

    ; Wczytaj komponenty R
    movzx eax, BYTE PTR [r12 + 2]    ; R0
    movzx ebx, BYTE PTR [r12 + 5]    ; R1
    movzx ecx, BYTE PTR [r12 + 8]    ; R2
    movzx edx, BYTE PTR [r12 + 11]   ; R3
    movd xmm2, eax
    pinsrd xmm2, ebx, 1
    pinsrd xmm2, ecx, 2
    pinsrd xmm2, edx, 3
    cvtdq2ps xmm2, xmm2              ; Konwertuj na float

    ; Mno¿enie przez wagi
    mulps xmm0, xmm14                ; B * Waga_B * jasnoœæ

    ; Dla komponentów G
    movss xmm3, DWORD PTR [rgb_weights + 4]    ; Za³aduj Waga_G
    mulss xmm3, xmm15                          ; Zastosuj jasnoœæ
    shufps xmm3, xmm3, 0                       ; Rozszerz do wszystkich elementów
    mulps xmm1, xmm3                           ; G * Waga_G * jasnoœæ

    ; Dla komponentów R
    movss xmm4, DWORD PTR [rgb_weights + 8]    ; Za³aduj Waga_R
    mulss xmm4, xmm15                          ; Zastosuj jasnoœæ
    shufps xmm4, xmm4, 0                       ; Rozszerz do wszystkich elementów
    mulps xmm2, xmm4                           ; R * Waga_R * jasnoœæ

    ; Sumuj komponenty
    addps xmm0, xmm1
    addps xmm0, xmm2

    ; Konwertuj na integer i zapakuj
    cvtps2dq xmm0, xmm0
    packssdw xmm0, xmm0
    packuswb xmm0, xmm0              ; Teraz mamy 16 bajtów, potrzebujemy najni¿szych 4 bajtów

    ; Zapisz wyniki do outputBuffer
    movd eax, xmm0                   ; Pobierz 4 bajty z xmm0

    ; Piksel 0
    mov bl, al
    mov [r13], bl
    mov [r13 + 1], bl
    mov [r13 + 2], bl

    ; Piksel 1
    mov bl, ah
    mov [r13 + 3], bl
    mov [r13 + 4], bl
    mov [r13 + 5], bl

    ; Piksel 2
    shr eax, 16
    mov bl, al
    mov [r13 + 6], bl
    mov [r13 + 7], bl
    mov [r13 + 8], bl

    ; Piksel 3
    mov bl, ah
    mov [r13 + 9], bl
    mov [r13 + 10], bl
    mov [r13 + 11], bl

    ; Przesuñ wskaŸniki
    add r12, 12      ; 4 piksele * 3 bajty
    add r13, 12

    ; Dekrementuj licznik bloków SIMD
    dec r15
    jne process_simd_loop

process_remaining_pixels:
    ; Przetwarzanie pozosta³ych pikseli
    cmp r14, 0
    je end_function

remaining_pixel_loop:
    ; Wczytaj komponenty B, G, R
    movzx eax, BYTE PTR [r12]      ; B
    movzx ebx, BYTE PTR [r12 + 1]  ; G
    movzx ecx, BYTE PTR [r12 + 2]  ; R

    ; Konwertuj na float
    cvtsi2ss xmm0, eax             ; B
    cvtsi2ss xmm1, ebx             ; G
    cvtsi2ss xmm2, ecx             ; R

    ; Mno¿enie przez wagi
    ; Dla B
    movss xmm3, DWORD PTR [rgb_weights]        ; Za³aduj Waga_B
    mulss xmm3, xmm15                          ; Zastosuj jasnoœæ
    mulss xmm0, xmm3                           ; B * Waga_B * jasnoœæ

    ; Dla G
    movss xmm3, DWORD PTR [rgb_weights + 4]    ; Za³aduj Waga_G
    mulss xmm3, xmm15                          ; Zastosuj jasnoœæ
    mulss xmm1, xmm3                           ; G * Waga_G * jasnoœæ

    ; Dla R
    movss xmm3, DWORD PTR [rgb_weights + 8]    ; Za³aduj Waga_R
    mulss xmm3, xmm15                          ; Zastosuj jasnoœæ
    mulss xmm2, xmm3                           ; R * Waga_R * jasnoœæ

    ; Sumuj komponenty
    addss xmm0, xmm1
    addss xmm0, xmm2

    ; Konwertuj na integer
    cvtss2si eax, xmm0

    ; Ogranicz zakres do 0-255
    xor ebx, ebx
    mov ecx, 255
    cmp eax, ebx
    cmovl eax, ebx
    cmp eax, ecx
    cmovg eax, ecx

    ; Zapisz wynik do outputBuffer
    mov bl, al
    mov [r13], bl
    mov [r13 + 1], bl
    mov [r13 + 2], bl

    ; Przesuñ wskaŸniki
    add r12, 3
    add r13, 3

    ; Dekrementuj licznik pikseli
    dec r14
    jne remaining_pixel_loop

end_function:
    ; Przywróæ rejestry nieulotne
    pop r15
    pop r14
    pop r13
    pop r12
    pop rbx

    ; Epilog funkcji
    mov rsp, rbp
    pop rbp
    ret

GrayscaleFilter ENDP
END
