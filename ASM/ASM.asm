.data
;zaczynamy od wyr�wnania do 16 bajt�w, 4 liczby x 4 bajty
;dane z tego przedzia�u s� zapisane w pami�ci, a nie jako sta�e wi�c musimy si� odnosi� do ich adres�w [], dzi�ki temu zmiana zmiennej nie wymaga �adnej ingerencji w kod
    align 16
    rounding_const REAL4 0.5, 0.5, 0.5, 0.5 ;zaokr�glenie, asm odcina cz�� przecinkow�
    max_value REAL4 255.0, 255.0, 255.0, 255.0 ;kolory przechowywane w bajtach wi�c od 0-255, bo 2^8
    min_value REAL4 0.0, 0.0, 0.0, 0.0

; mamy super maszyn�(rejestry xmm), kt�ra miesza 4 zestawy farb jednocze�nie, a nie 1, real4 - w�� tu do r_weight 4 liczby z przecinkiem
    align 16
    r_weight REAL4 0.299, 0.299, 0.299, 0.299
    g_weight REAL4 0.587, 0.587, 0.587, 0.587
    b_weight REAL4 0.114, 0.114, 0.114, 0.114

.code
GrayscaleFilter PROC
;zachowujemy warto�ci rejestr�w, �eby nie popsu� innych program�w
    push rbp ;dodaje element na stos, zwi�ksza rsp, troche takie zapami�tanie "tu ko�czy�y si� dane z poprzedniego programu"
    mov rbp, rsp ;z rsp do rbp pokazuj� gdzie "zaczynamy"
    push rsi ;rejestr do czytania danych ze starego obrazka, mo�e mie� ju� dane wi�c zapisujemy je na stosie (og�lnie "source index")
    push rdi ;rejestr do zapisywania nowego obrazka ("destination index")
    push rbx ;rejestr do liczenia ile zosta�o pikseli

    mov rsi, rcx ;wg Microsoft x64 calling convention rcx dostaje pierwszy parametr tzn adres obrazka starego, kopiujemy do rsi, rejestry rcx i rdx s� u�ywane do przekazywanie parametr�w     
    mov rdi, rdx ;rdx dostaje drugi parametr tzn destination index w naszym przypadku     
    mov ebx, r8d ; r8d dostaje trzeci parametr tzn. liczb� pikseli
    ; jest jeszcze 4 parametr r9

    shufps xmm3, xmm3, 0 ;bierzemy pierwsz� cz�� xmm3 i kopiujemy j�  do wszystkich cz�ci rejestru 

    movaps xmm7, XMMWORD PTR [rounding_const] ;bierze 16 bajt�w z adresu r_c i �aduje do xmm7 
    movaps xmm8, XMMWORD PTR [max_value]       
    movaps xmm9, XMMWORD PTR [min_value]       
    
   
    movaps xmm10, XMMWORD PTR [r_weight]      
    movaps xmm11, XMMWORD PTR [g_weight]      
    movaps xmm12, XMMWORD PTR [b_weight]       
    
    mulps xmm10, xmm3 ;musi by� waga x jasno��, parametryzacja jasno�ci                        
    mulps xmm11, xmm3
    mulps xmm12, xmm3

    mov ecx, ebx ; kopiujemy liczb� pikseli w celu modyfikacji tego rejestru
    shr ecx, 2 ; dzielimy przez 4, przesuwamy dwa bity w prawo tzn 1110->0011        
    test ecx, ecx ;sprawdzamy czy wynik jest 0 
    jz process_remaining; je�li zero skaczemy lecimy do pozosta�ych pikseli

process_4_pixels:
;�adujemy warto�ci niebieskiego z 4 r�nych pikseli
;�eby nie by�o za �atwo zamiast RGB mamy BGR, ze wzgl�du standardu VGA(Video Graphics Array) wprowadzonego przez IBM(jaka� stara firma, co stworzy�a pierwszy popularny komputer)
    movzx eax, byte ptr [rsi] ;bierze niebieski kolor (pierwszy bajt spod adresu rsi), eax ma 32 bajty  
    cvtsi2ss xmm0, eax ;zamieniamy liczb� ca�kowit� na liczb� z przecinkiem
    movzx eax, byte ptr [rsi+3];bierzemy z offset�w 0,3,6,9, movzx dzia�a tylko z rejestrami og�lnego przeznaczenia.
    cvtsi2ss xmm1, eax
    movzx eax, byte ptr [rsi+6] 
    cvtsi2ss xmm2, eax
    movzx eax, byte ptr [rsi+9]  
    cvtsi2ss xmm4, eax
    ;sk�adanie wszystkich warto�ci niebieskiego w jeden rejestr
    ;teraz jest np xmm0=[B1, , ,]; xmm1=[B2, , ,], a b�dzie xmm0=[B1,B2,..,...]
    insertps xmm0, xmm1, 010h ;wstawia warto�� z xmm1 na drug� pozycj� xmm0, system szesnatskowy, 0001 0000, patrzy si� na 4-5 bit, tzn 01
    insertps xmm0, xmm2, 020h
    insertps xmm0, xmm4, 030h
    
    mulps xmm0, xmm12 ;mno�y warto�ci niebieskie razy ich wag�

    ;to samo dla zielonego
    movzx eax, byte ptr [rsi+1] 
    cvtsi2ss xmm1, eax
    movzx eax, byte ptr [rsi+4] 
    cvtsi2ss xmm2, eax
    movzx eax, byte ptr [rsi+7]  
    cvtsi2ss xmm4, eax
    movzx eax, byte ptr [rsi+10]
    cvtsi2ss xmm5, eax
    insertps xmm1, xmm2, 010h
    insertps xmm1, xmm4, 020h
    insertps xmm1, xmm5, 030h
    
    mulps xmm1, xmm11 ;mno�ymy warto�ci zielone razy ich wag�            
    addps xmm0, xmm1 ;dodajemy 4 liczby jednocze�nie, dodajemy do siebie niebieski i zielony, bo tak odbiera ludzkie oko, tylko % danego koloru potrzebuje w pikselu by zmieni� jego barw� na szar�           

   ;to samo dla czerwonego
    movzx eax, byte ptr [rsi+2]
    cvtsi2ss xmm1, eax
    movzx eax, byte ptr [rsi+5] 
    cvtsi2ss xmm2, eax
    movzx eax, byte ptr [rsi+8]  
    cvtsi2ss xmm4, eax
    movzx eax, byte ptr [rsi+11]  
    cvtsi2ss xmm5, eax
    insertps xmm1, xmm2, 010h
    insertps xmm1, xmm4, 020h
    insertps xmm1, xmm5, 030h
    
    mulps xmm1, xmm10      
    addps xmm0, xmm1     

    addps xmm0, xmm7 ;dodajemy to pi�kne zaokr�glenie, super wtedy zmienimy na inta            
    maxps xmm0, xmm9 ;pilnujemy by nie wyj�� po za max 255             
    minps xmm0, xmm8 ;i po za min 0. jak bedzie mniej to bedzie 0, jak wiecej to 255           

   
    cvtss2si eax, xmm0 ;zamiana na inta, zapisuje si� do dolnej cz�ci rejestru eax
    ;bgr, sk�adowe musz� by� takie same bo to daje jednolity odcie� szaro�ci, dla jednego piksela 
    mov byte ptr [rdi], al ; zawiera warto�� do zapisania, 8 dolnych bit�w rejestru eax, przechowuje sk�adow� koloru piksela w trakcie oblicze�, ma 1 bajt - 8 bit�w wi�c idealnie tak jak s� zapisane kolory
    mov byte ptr [rdi+1], al; al do adresu jednego bajta [rdi +1]
    mov byte ptr [rdi+2], al
    
    psrldq xmm0, 4
    cvtss2si eax, xmm0 ;dane zostaj� nadpisane wi�c mo�na znowu u�y� al, �eby by�o bardziej optymalnie 
    mov byte ptr [rdi+3], al
    mov byte ptr [rdi+4], al
    mov byte ptr [rdi+5], al
    
    psrldq xmm0, 4 ; przesuwamy o 4 bajty, xmm0 ma 128 bit�w (16 bajt�w), 4 odcienie szarego, 4 piksele wi�c musimy o 4 bajty przesun�� by dosta� si� do kolejnego odcienia 
    cvtss2si eax, xmm0 ;zamieniamy ten odcie� na inta
    mov byte ptr [rdi+6], al 
    mov byte ptr [rdi+7], al
    mov byte ptr [rdi+8], al
    
    psrldq xmm0, 4
    cvtss2si eax, xmm0
    mov byte ptr [rdi+9], al
    mov byte ptr [rdi+10], al
    mov byte ptr [rdi+11], al

    add rsi, 12  ;piksel zajmuje 3 bajty wi�c zwi�kszany rsi o 12                 
    add rdi, 12
    dec ecx ;odejmujemy, bo zrobili�my
    jnz process_4_pixels ;je�li wynik poprzedniej operacji nie jest 0 to skaczemy do process_4_pixels znowu przetwarzamy 4 piksele

    ;czy s� pozosta�e piksele?
process_remaining:
    and ebx, 3  ; operacja and mi�dzy ebx a 11, u�ywamy maski wynik and zawiera najmniej znacz�ce dwa bity tzn 1101->01, 1000->00, 0011->11, 0110->10               
    jz cleanup ;jump if zero do cleanupa posprz�ta�

    movss xmm6, xmm3 ;przenosimy liczb� zmiennoprzecinkow� 

;przetw�rzmy je
remaining_loop:             
    movzx eax, byte ptr [rsi] ;jak wy�ej bierze sobie niebieski, daje go do eax
    cvtsi2ss xmm1, eax ;zamiana na liczb� z przecinkiem i zapis do xmm1
    mulss xmm1, dword ptr [b_weight] ;mno�ymy xmm1 przez wag� koloru niebieskiego pod adresem [b_weight]
    mulss xmm1, xmm6 ;mno�ymy razy jasno��            
    
    movzx eax, byte ptr [rsi+1];G
    cvtsi2ss xmm2, eax
    mulss xmm2, dword ptr [g_weight]
    mulss xmm2, xmm6         
    addss xmm1, xmm2
    
    movzx eax, byte ptr [rsi+2];R
    cvtsi2ss xmm2, eax
    mulss xmm2, dword ptr [r_weight]
    mulss xmm2, xmm6       
    addss xmm1, xmm2
    
    addss xmm1, dword ptr [rounding_const]
    maxss xmm1, dword ptr [min_value]
    minss xmm1, dword ptr [max_value]
    
    cvtss2si eax, xmm1 ;konwertujemy na liczb� ca�kowit�
    mov byte ptr [rdi], al ;B
    mov byte ptr [rdi+1], al ;G
    mov byte ptr [rdi+2], al ;R
    
    add rsi, 3 ; zwiekszamy o 1 piksel
    add rdi, 3
    dec ebx 
    jnz remaining_loop

cleanup:
;przywracamy stare warto�ci rejestr�w :)
    pop rbx
    pop rdi
    pop rsi
    pop rbp
    ret ;ko�czymy asemblera.:)
GrayscaleFilter ENDP
END