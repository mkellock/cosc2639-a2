import Login, { LoginDetails } from './Components/Login';
import Register, { RegisterDetails } from './Components/Register';
import MainUI, { UserDetails } from './Components/MainUI';
import { useState } from 'react';
import { ApolloClient, InMemoryCache, gql } from '@apollo/client';
import { ChakraProvider } from '@chakra-ui/react';

export function App() {
    const [userDetails, setUserDetails] = useState<UserDetails>();
    const [loginHidden, setLoginHidden] = useState(false);
    const [registerHidden, setRegisterHidden] = useState(true);
    const [mainUIHidden, setMainUIHidden] = useState(true);
    const [loginInError, setLoginInError] = useState(false);
    const [logInErrorMessage, setLogInErrorMessage] = useState<string>();
    const [registerError, setRegisterError] = useState(false);
    const [registerErrorMessage, setRegisterInErrorMessage] = useState<string>();

    let gqlUri = '/graphql';

    const client = new ApolloClient({
        uri: gqlUri,
        cache: new InMemoryCache(),
    });

    const onLoginSubmit = (loginDetails: LoginDetails) => {
        // Call API to authenticate user
        const getLoginDetails = gql`
            query userByEmailPassword($email: String!, $password: String!) {
                userByEmailPassword(email: $email, password: $password) {
                    username
                }
            }
        `;

        const loginVariables = {
            email: loginDetails.email,
            password: loginDetails.password,
        };

        client
            .query({
                query: getLoginDetails,
                variables: loginVariables,
            })
            .then((result) => {
                // If login is unsuccessful
                if (result.data.userByEmailPassword === null) {
                    setLoginInError(true);
                    setLogInErrorMessage('Email or password is invalid');
                } else {
                    // If authentication passes
                    setMainUIHidden(false);
                    setLoginHidden(true);
                    setLoginInError(false);
                    setLogInErrorMessage('');

                    // Load the user's data
                    loadUser({ ...result.data.userByEmailPassword });
                }
            });
    };

    const onLogin = () => {
        setLoginInError(false);
        setLogInErrorMessage('');
        setLoginHidden(false);
        setRegisterHidden(true);
    };

    const onRegister = () => {
        setLoginHidden(true);
        setRegisterHidden(false);
    };

    const onRegisterSubmit = (registerDetails: RegisterDetails) => {
        // Call API to register user
        const registerUser = gql`
            mutation registerUser($email: String!, $username: String!, $password: String!) {
                registerUser(email: $email, username: $username, password: $password)
            }
        `;

        const registerUserVariables = {
            email: registerDetails.email,
            username: registerDetails.username,
            password: registerDetails.password
        };

        client
            .mutate({
                mutation: registerUser,
                variables: registerUserVariables,
            })
            .then((result) => {
                if (result.data.registerUser) {
                    // If registration passes
                    setRegisterHidden(true);
                    setRegisterError(false);

                    // Go back to the login screen
                    onLogoutClick();
                } else {
                    // If id exists
                    setRegisterError(true);
                    setRegisterInErrorMessage('The EMail already exists');
                }
            });
    };

    const onLogoutClick = () => {
        // Set up the UI
        setMainUIHidden(true);
        setLoginHidden(false);

        // Unset the user's login
        setUserDetails(undefined);
    };

    const loadUser = (userData: UserDetails) => {
        // Set the login details
        setUserDetails({ ...userData });
    };

    return (
        <ChakraProvider>
            <Login onSubmit={onLoginSubmit} onRegister={onRegister} hidden={loginHidden} inError={loginInError} errorMessage={logInErrorMessage} />
            <Register hidden={registerHidden} onSubmit={onRegisterSubmit} inError={registerError} errorMessage={registerErrorMessage} onLogin={onLogin} />
            <MainUI hidden={mainUIHidden} onLogoutClick={onLogoutClick} userDetails={userDetails}>
                
            </MainUI>
        </ChakraProvider>
    );
}

export default App;